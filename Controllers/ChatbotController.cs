using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TutorConnect.WebApp.Models;
using TutorConnect.WebApp.Services;

namespace TutorConnect.WebApp.Controllers
{
    [Authorize] // ✅ FIXED: Re-enable authorization
    [Route("Chatbot")] // ✅ ADDED: Explicit route for cleaner URLs
    public class ChatbotController : Controller
    {
        private readonly ApiService _apiService;
        private readonly ILogger<ChatbotController> _logger;

        public ChatbotController(ApiService apiService, ILogger<ChatbotController> logger)
        {
            _apiService = apiService;
            _logger = logger;
        }

        // GET: /Chatbot - Main chatbot page
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        // POST: /Chatbot/SendMessage - Send message to chatbot
        [HttpPost("SendMessage")]
        public async Task<IActionResult> SendMessage([FromBody] ChatMessageRequest request)
        {
            try
            {
                _logger.LogInformation("=== WEBAPP CHATBOT REQUEST ===");

                // Get current user info
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;

                _logger.LogInformation("User - ID: {UserId}, Email: {Email}", userId, userEmail);
                _logger.LogInformation("Question: {Question}", request.Question);
                _logger.LogInformation("ConversationId: {ConversationId}", request.ConversationId);

                // Validate request
                if (string.IsNullOrWhiteSpace(request.Question))
                {
                    return BadRequest(new { error = "Question cannot be empty" });
                }

                // Call API service
                var response = await _apiService.AskChatbotAsync(
                    request.Question,
                    request.ConversationId,
                    request.Context
                );

                _logger.LogInformation("✅ Chatbot response received: {Length} chars", response?.Answer?.Length ?? 0);
                return Ok(response);
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "❌ API request failed");
                return StatusCode(500, new
                {
                    error = "Sorry, I'm having trouble connecting to the chatbot service. Please try again.",
                    details = httpEx.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Unexpected error in chatbot");
                return StatusCode(500, new
                {
                    error = "Sorry, something went wrong. Please try again.",
                    details = ex.Message
                });
            }
        }

        // GET: /Chatbot/Conversations - Get conversation history
        [HttpGet("Conversations")]
        public async Task<IActionResult> GetConversations()
        {
            try
            {
                _logger.LogInformation("Getting chatbot conversations");
                var conversations = await _apiService.GetChatbotConversationsAsync();
                return Ok(conversations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversations");
                // Return empty array instead of error for better UX
                return Ok(new List<ConversationDTO>());
            }
        }

        // DELETE: /Chatbot/Conversations/{conversationId} - Delete a conversation
        [HttpDelete("Conversations/{conversationId}")]
        public async Task<IActionResult> DeleteConversation(int conversationId)
        {
            try
            {
                _logger.LogInformation("Deleting conversation {ConversationId}", conversationId);
                var success = await _apiService.DeleteChatbotConversationAsync(conversationId);

                if (success)
                {
                    return Ok(new { success = true, message = "Conversation deleted" });
                }

                return BadRequest(new { success = false, message = "Failed to delete conversation" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting conversation");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }

    // Request model for SendMessage endpoint
    public class ChatMessageRequest
    {
        public string Question { get; set; }
        public int? ConversationId { get; set; }
        public string Context { get; set; }
    }
}