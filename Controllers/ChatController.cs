using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using TutorConnect.WebApp.Models;
using TutorConnect.WebApp.Services;

namespace TutorConnect.WebApp.Controllers
{
    public class ChatController : Controller
    {
        private readonly ApiService _apiService;

        public ChatController(ApiService apiService)
        {
            _apiService = apiService;
        }

        // Loads chat page
        [HttpGet]
        public IActionResult Chat()
        {
            return View(); // returns Chat.cshtml
        }

        // Returns all users except current user
       

        [HttpGet]
        public async Task<IActionResult> Inbox()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int currentUserId))
                return Unauthorized();

            var users = await _apiService.GetChatUsersAsync();

            // Remove current user
            users = users.Where(u => u.UserId != currentUserId).ToList();

            // Return as JSON for the view
            var jsonUsers = users.Select(u => new
            {
                userId = u.UserId,
                name = string.IsNullOrEmpty(u.Name) ? u.UserId.ToString() : u.Name
            }).ToList();

            return Json(jsonUsers);
        }


        // Returns chat messages between current user and selected user
        [HttpGet]
        public async Task<IActionResult> GetChatHistory(int otherUserId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int currentUserId))
                return Unauthorized();

            // Mark messages as read
            await _apiService.MarkMessagesAsReadAsync(otherUserId);

            var messages = await _apiService.GetChatHistoryAsync(otherUserId);

            var jsonMessages = messages.Select(m => new
            {
                senderId = m.SenderId,
                message = m.Content,
                sentAt = m.SentAt
            }).ToList();

            return Json(jsonMessages);
        }




    }
}
