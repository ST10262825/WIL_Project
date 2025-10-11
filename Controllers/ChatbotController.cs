using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TutorConnectAPI.Data;
using TutorConnectAPI.DTOs;
using TutorConnectAPI.Models;
using TutorConnectAPI.Services;

namespace TutorConnectAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize]
    public class ChatbotController : ControllerBase
    {
        private readonly IChatbotService _chatbotService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<GamificationController> _logger;

        public ChatbotController(IChatbotService chatbotService, ApplicationDbContext context,
            ILogger<GamificationController> logger)
        {
            _chatbotService = chatbotService;
            _context = context;
            _logger = logger;
        }

        [HttpPost("ask")]
        public async Task<IActionResult> AskQuestion([FromBody] ChatQuestionRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var response = await _chatbotService.ProcessQuestionAsync(request, userId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error processing question: {ex.Message}");
            }
        }

        [HttpGet("conversations")]
        public async Task<IActionResult> GetConversations()
        {
            try
            {
                var userId = GetCurrentUserId();
                var conversations = await _chatbotService.GetUserConversationsAsync(userId);
                return Ok(conversations);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving conversations: {ex.Message}");
            }
        }

        [HttpDelete("conversations/{conversationId}")]
        public async Task<IActionResult> DeleteConversation(int conversationId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var success = await _chatbotService.DeleteConversationAsync(conversationId, userId);
                return Ok(new { success });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error deleting conversation: {ex.Message}");
            }
        }

        [HttpPost("train")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> TrainKnowledgeBase()
        {
            try
            {
                await _chatbotService.TrainKnowledgeBaseAsync();
                return Ok(new { message = "Knowledge base trained successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error training knowledge base: {ex.Message}");
            }
        }

        // ADD THIS TEST ENDPOINT TEMPORARILY
        [HttpGet("test")]
        [AllowAnonymous]
        public IActionResult Test()
        {
            return Ok(new
            {
                message = "Chatbot API is working!",
                timestamp = DateTime.UtcNow,
                endpoints = new[] {
                    "POST /api/chatbot/ask",
                    "GET /api/chatbot/conversations",
                    "DELETE /api/chatbot/conversations/{id}",
                    "POST /api/chatbot/train"
                }
            });
        }

        private int GetCurrentUserId()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userIdClaim))
                {
                    _logger.LogWarning("User ID claim not found. Available claims: {Claims}",
                        string.Join(", ", User.Claims.Select(c => $"{c.Type}: {c.Value}")));
                    throw new UnauthorizedAccessException("User ID not found in claims");
                }

                if (int.TryParse(userIdClaim, out int userId))
                {
                    _logger.LogInformation("Successfully parsed user ID: {UserId}", userId);
                    return userId;
                }
                else
                {
                    _logger.LogError("Failed to parse user ID from claim: {UserIdClaim}", userIdClaim);
                    throw new UnauthorizedAccessException($"Unable to parse user ID from claim: {userIdClaim}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetCurrentUserId");
                throw;
            }
        }

        [HttpPost("test-simple")]
        [Authorize]
        public async Task<IActionResult> TestSimple()
        {
            Console.WriteLine($"=== TEST SIMPLE STARTED ===");

            try
            {
                var userId = GetCurrentUserId();
                Console.WriteLine($"User ID: {userId}");

                // Test 1: Check if user exists
                var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId);
                Console.WriteLine($"User exists: {user != null}");

                if (user == null)
                {
                    return BadRequest($"User {userId} not found in database");
                }

                // Test 2: Check database connection and table access
                var canConnect = await _context.Database.CanConnectAsync();
                Console.WriteLine($"Database connected: {canConnect}");

                var conversationsCount = await _context.ChatbotConversations.CountAsync();
                var messagesCount = await _context.ChatbotMessages.CountAsync();
                Console.WriteLine($"Conversations: {conversationsCount}, Messages: {messagesCount}");

                // Test 3: Try a SIMPLE insert without any complex objects
                Console.WriteLine("Testing simple insert...");

                var simpleConversation = new ChatbotConversation
                {
                    UserId = userId,
                    Title = "Simple Test " + DateTime.UtcNow.Ticks,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                Console.WriteLine("Created conversation object, about to add to context...");
                _context.ChatbotConversations.Add(simpleConversation);

                Console.WriteLine("About to save changes...");
                await _context.SaveChangesAsync();
                Console.WriteLine($"✅ Simple conversation saved with ID: {simpleConversation.ConversationId}");

                // Test 4: Try to add a simple message
                // Test 4: Try to add a simple message
                var simpleMessage = new ChatbotMessage
                {
                    ConversationId = simpleConversation.ConversationId,
                    Content = "Test message at " + DateTime.UtcNow.ToString("HH:mm:ss"),
                    IsUserMessage = true,
                    SentAt = DateTime.UtcNow,
                    MessageType = "text",
                    Metadata = null // Explicitly set to null
                };

                Console.WriteLine("Created message object, about to add to context...");
                _context.ChatbotMessages.Add(simpleMessage);

                Console.WriteLine("About to save message changes...");
                await _context.SaveChangesAsync();
                Console.WriteLine($"✅ Simple message saved with ID: {simpleMessage.MessageId}");

                // Test 5: Clean up
                Console.WriteLine("Cleaning up test data...");
                _context.ChatbotMessages.Remove(simpleMessage);
                _context.ChatbotConversations.Remove(simpleConversation);
                await _context.SaveChangesAsync();
                Console.WriteLine("✅ Test data cleaned up");

                Console.WriteLine($"=== TEST SIMPLE COMPLETED SUCCESSFULLY ===");

                return Ok(new
                {
                    success = true,
                    message = "All tests passed!",
                    userId = userId,
                    testConversationId = simpleConversation.ConversationId,
                    testMessageId = simpleMessage.MessageId
                });
            }
            catch (DbUpdateException dbEx)
            {
                Console.WriteLine($"❌ DATABASE UPDATE EXCEPTION: {dbEx.Message}");
                Console.WriteLine($"❌ ENTITY FRAMEWORK ENTRIES:");

                if (dbEx.Entries != null)
                {
                    foreach (var entry in dbEx.Entries)
                    {
                        Console.WriteLine($"❌ Entry: {entry.Entity.GetType().Name}, State: {entry.State}");
                        foreach (var property in entry.Properties)
                        {
                            Console.WriteLine($"❌   {property.Metadata.Name}: {property.CurrentValue} (IsModified: {property.IsModified})");
                        }
                    }
                }

                if (dbEx.InnerException != null)
                {
                    Console.WriteLine($"❌ INNER EXCEPTION: {dbEx.InnerException.Message}");
                }

                return StatusCode(500, new
                {
                    error = "Database update failed",
                    message = dbEx.Message,
                    innerMessage = dbEx.InnerException?.Message
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ GENERAL EXCEPTION: {ex.Message}");
                Console.WriteLine($"❌ STACK TRACE: {ex.StackTrace}");

                return StatusCode(500, new
                {
                    error = "Test failed",
                    message = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        [HttpPost("minimal")]
        [Authorize]
        public async Task<IActionResult> MinimalTest([FromBody] ChatQuestionRequest request)
        {
            Console.WriteLine($"MINIMAL TEST - Question: {request.Question}");

            try
            {
                // Bypass all database operations and return a simple response
                var response = new ChatResponse
                {
                    Answer = $"**This is a test response!**\n\nI received your question: \"{request.Question}\"\n\nThis confirms the chatbot is working at a basic level.",
                    ConversationId = 999,
                    MessageId = 999,
                    Suggestions = new List<QuickSuggestion>
            {
                new QuickSuggestion { Text = "Ask about booking", Type = "question" },
                new QuickSuggestion { Text = "Contact support", Type = "action", Action = "mailto:support@tutorconnect.com" }
            },
                    RelevantDocs = new List<RelevantDocument>(),
                    RequiresHumanSupport = false
                };

                Console.WriteLine($"MINIMAL TEST - Successfully returning response");
                return Ok(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MINIMAL TEST FAILED: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("debug-claims")]
        [Authorize]
        public IActionResult DebugClaims()
        {
            var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
            return Ok(new
            {
                claims,
                userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                emailClaim = User.FindFirst(ClaimTypes.Email)?.Value,
                nameClaim = User.FindFirst(ClaimTypes.Name)?.Value
            });
        }
    }
}