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
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int currentUserId))
                {
                    Console.WriteLine("Unauthorized: No user ID claim found");
                    return Unauthorized();
                }

                Console.WriteLine($"Loading chat users for user ID: {currentUserId}");

                var users = await _apiService.GetChatUsersAsync();
                Console.WriteLine($"Retrieved {users?.Count ?? 0} users from API");

                if (users == null || users.Count == 0)
                {
                    Console.WriteLine("No users returned from API - user has no bookings yet");
                    return Json(new List<object>());
                }

                // Users are already sorted by the API, just return them
                Console.WriteLine($"Returning {users.Count} users");
                return Json(users);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Inbox Error: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");

                // Return empty list on error
                return Json(new List<object>());
            }
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

        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int senderId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int currentUserId))
                    return Unauthorized();

                await _apiService.MarkMessagesAsReadAsync(senderId);
                return Ok();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MarkAsRead Error: {ex.Message}");
                return StatusCode(500);
            }
        }


    }
}
