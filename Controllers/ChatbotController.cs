using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
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
        private readonly IClaudeAIService _claudeAIService;
        private readonly IConfiguration _configuration;


        public ChatbotController(IChatbotService chatbotService, ApplicationDbContext context,
            ILogger<GamificationController> logger, IClaudeAIService claudeAIService, IConfiguration configuration)
        {
            _chatbotService = chatbotService;
            _context = context;
            _logger = logger;
            _claudeAIService = claudeAIService;
            _configuration = configuration;
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




        [HttpPost("test-claude")]
       
        public async Task<IActionResult> TestClaude([FromBody] ChatQuestionRequest request)
        {
            try
            {
                _logger.LogInformation("Testing Claude with question: {Question}", request.Question);

                var response = await _claudeAIService.GenerateChatResponseAsync(request.Question);

                _logger.LogInformation("Claude response: {Response}", response);

                return Ok(new
                {
                    success = !string.IsNullOrEmpty(response),
                    response = response ?? "Claude returned empty response",
                    message = !string.IsNullOrEmpty(response) ? "Claude is working!" : "Claude failed to respond",
                    responseLength = response?.Length ?? 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing Claude");
                return StatusCode(500, new
                {
                    error = ex.Message,
                    innerError = ex.InnerException?.Message
                });
            }
        }

        // Add this to your ChatbotController.cs

        [HttpPost("test-claude-detailed")]
   
        public async Task<IActionResult> TestClaudeDetailed([FromBody] ChatQuestionRequest request)
        {
            try
            {
                _logger.LogInformation("=== DETAILED CLAUDE TEST STARTED ===");

                // Test 1: Check configuration
                var apiKey = _configuration["Claude:ApiKey"];
                var model = _configuration["Claude:Model"];

                var configTest = new
                {
                    hasApiKey = !string.IsNullOrEmpty(apiKey),
                    apiKeyLength = apiKey?.Length ?? 0,
                    apiKeyPrefix = apiKey?.Substring(0, Math.Min(15, apiKey?.Length ?? 0)),
                    model = model,
                    maxTokens = _configuration["Claude:MaxTokens"],
                    temperature = _configuration["Claude:Temperature"]
                };

                _logger.LogInformation("Config: {@Config}", configTest);

                // Test 2: Manual HTTP call to Claude
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
                httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

                var requestBody = new
                {
                    model = "claude-3-5-sonnet-20241022",
                    max_tokens = 1024,
                    messages = new[]
                    {
                new
                {
                    role = "user",
                    content = "Hi! Please respond with just 'Hello!'"
                }
            }
                };

                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                };

                var json = JsonSerializer.Serialize(requestBody, jsonOptions);
                _logger.LogInformation("Request JSON:\n{Json}", json);

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = "https://api.anthropic.com/v1/messages";
                _logger.LogInformation("Calling: {Url}", url);

                var response = await httpClient.PostAsync(url, content);

                var responseContent = await response.Content.ReadAsStringAsync();
                var responseHeaders = response.Headers.ToString();

                _logger.LogInformation("Status: {Status}", response.StatusCode);
                _logger.LogInformation("Response:\n{Response}", responseContent);

                // Test 3: Try with ClaudeAIService
                string claudeServiceResponse = null;
                try
                {
                    claudeServiceResponse = await _claudeAIService.GenerateChatResponseAsync(request.Question);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "ClaudeAIService failed");
                }

                return Ok(new
                {
                    configTest,
                    directApiCall = new
                    {
                        url,
                        statusCode = (int)response.StatusCode,
                        statusName = response.StatusCode.ToString(),
                        isSuccess = response.IsSuccessStatusCode,
                        responsePreview = responseContent?.Length > 500
                            ? responseContent.Substring(0, 500) + "..."
                            : responseContent,
                        responseLength = responseContent?.Length ?? 0
                    },
                    claudeServiceResult = new
                    {
                        response = claudeServiceResponse,
                        length = claudeServiceResponse?.Length ?? 0
                    },
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Detailed test failed");
                return StatusCode(500, new
                {
                    error = ex.Message,
                    stackTrace = ex.StackTrace,
                    innerError = ex.InnerException?.Message
                });
            }
        }


        // Add this endpoint to your ChatbotController to test Claude without DB operations

        [HttpPost("test-claude-simple")]
        [Authorize]
        public async Task<IActionResult> TestClaudeSimple([FromBody] ChatQuestionRequest request)
        {
            try
            {
                _logger.LogInformation("=== SIMPLE CLAUDE TEST ===");
                _logger.LogInformation("Question: {Question}", request.Question);

                // Build a simple prompt like in ChatbotService
                var prompt = $@"
You are TutorBot, an AI assistant for TutorConnect - a tutoring platform connecting students with tutors.

PLATFORM CONTEXT:
- TutorConnect helps students find tutors for various subjects
- Users can be Students or Tutors
- Sessions can be booked, rescheduled, or cancelled
- Tutors have profiles with ratings and module expertise
- Students can book sessions and leave reviews

Instructions:
1. Answer specifically about TutorConnect platform features and processes
2. Be helpful, friendly, and concise
3. Provide specific steps or guidance when possible
4. Use markdown formatting for better readability

USER QUESTION: {request.Question}
";

                _logger.LogInformation("Calling Claude with prompt length: {Length}", prompt.Length);

                var response = await _claudeAIService.GenerateChatResponseAsync(prompt);

                _logger.LogInformation("Claude response received: {HasResponse}", !string.IsNullOrEmpty(response));
                _logger.LogInformation("Response length: {Length}", response?.Length ?? 0);
                _logger.LogInformation("Response content: {Response}", response);

                return Ok(new
                {
                    success = !string.IsNullOrEmpty(response),
                    question = request.Question,
                    answer = response,
                    answerLength = response?.Length ?? 0,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Simple Claude test failed");
                return StatusCode(500, new
                {
                    error = ex.Message,
                    stackTrace = ex.StackTrace,
                    innerError = ex.InnerException?.Message
                });
            }
        }

        // Add this debugging version of the /ask endpoint to your ChatbotController

        [HttpPost("ask-debug")]
       
        public async Task<IActionResult> AskDebug([FromBody] ChatQuestionRequest request)
        {
            var debugInfo = new List<string>();

            try
            {
                debugInfo.Add("1. Starting request");
                var userId = GetCurrentUserId();
                debugInfo.Add($"2. User ID: {userId}");

                // Check user exists
                var user = await _context.Users.FindAsync(userId);
                debugInfo.Add($"3. User found: {user != null}");

                if (user == null)
                {
                    return BadRequest(new { error = "User not found", debug = debugInfo });
                }

                // Check if conversationId is valid
                debugInfo.Add($"4. ConversationId: {request.ConversationId}");

                // Try to call the chatbot service with detailed error catching
                try
                {
                    debugInfo.Add("5. Calling chatbot service...");
                    var response = await _chatbotService.ProcessQuestionAsync(request, userId);
                    debugInfo.Add("6. Chatbot service completed successfully");
                    debugInfo.Add($"7. Response length: {response.Answer?.Length ?? 0}");

                    return Ok(new
                    {
                        success = true,
                        response = response,
                        debug = debugInfo
                    });
                }
                catch (DbUpdateException dbEx)
                {
                    debugInfo.Add($"❌ Database update error: {dbEx.Message}");
                    debugInfo.Add($"❌ Inner: {dbEx.InnerException?.Message}");

                    _logger.LogError(dbEx, "Database error in ProcessQuestionAsync");

                    return StatusCode(500, new
                    {
                        error = "Database error",
                        message = dbEx.Message,
                        innerMessage = dbEx.InnerException?.Message,
                        debug = debugInfo
                    });
                }
                catch (Exception serviceEx)
                {
                    debugInfo.Add($"❌ Service error: {serviceEx.Message}");
                    debugInfo.Add($"❌ Type: {serviceEx.GetType().Name}");

                    _logger.LogError(serviceEx, "Error in ProcessQuestionAsync");

                    return StatusCode(500, new
                    {
                        error = "Service error",
                        message = serviceEx.Message,
                        stackTrace = serviceEx.StackTrace,
                        debug = debugInfo
                    });
                }
            }
            catch (Exception ex)
            {
                debugInfo.Add($"❌ Controller error: {ex.Message}");

                _logger.LogError(ex, "Error in AskDebug endpoint");

                return StatusCode(500, new
                {
                    error = "Controller error",
                    message = ex.Message,
                    stackTrace = ex.StackTrace,
                    debug = debugInfo
                });
            }
        }

    }
}