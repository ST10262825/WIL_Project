using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TutorConnect.WebApp.Services;

namespace TutorConnect.WebApp.Controllers
{
    //[Authorize]
    public class ChatbotController : Controller
    {
        private readonly ApiService _apiService;

        public ChatbotController(ApiService apiService)
        {
            _apiService = apiService;
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] ChatMessageRequest request)
        {
            try
            {
                // Get current user info for debugging
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;

                Console.WriteLine($"WebApp Chatbot - UserId: {userId}, Email: {userEmail}");

                var response = await _apiService.AskChatbotAsync(
                    request.Question,
                    request.ConversationId,
                    request.Context
                );

                return Ok(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WebApp Chatbot Error: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");

                return StatusCode(500, new
                {
                    error = "Sorry, I'm having trouble connecting right now. Please try again.",
                    details = ex.Message
                });
            }
        }


        [HttpGet]
        public async Task<IActionResult> GetConversations()
        {
            try
            {
                var conversations = await _apiService.GetChatbotConversationsAsync();
                return Ok(conversations);
            }
            catch (Exception ex)
            {
                // Return empty array instead of error for better UX
                return Ok(new List<object>());
            }
        }
    }

    public class ChatMessageRequest
    {
        public string Question { get; set; }
        public int? ConversationId { get; set; }
        public string Context { get; set; }
    }
}