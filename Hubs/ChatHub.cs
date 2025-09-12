using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using TutorConnectAPI.Data;
using TutorConnectAPI.Models;

namespace TutorConnectAPI.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext _context;

        public ChatHub(ApplicationDbContext context)
        {
            _context = context;
        }

        // ----------------------------
        // Send a private message
        // ----------------------------
        public async Task SendMessage(int senderId, int receiverId, string message)
        {
            var chatMessage = new ChatMessage
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Message = message,
                SentAt = DateTime.UtcNow,
                IsRead = false // make sure unread by default
            };

            _context.ChatMessages.Add(chatMessage);
            await _context.SaveChangesAsync();

            // Send to receiver and sender
            await Clients.Group(receiverId.ToString())
                         .SendAsync("ReceiveMessage", chatMessage);

            await Clients.Group(senderId.ToString())
                         .SendAsync("ReceiveMessage", chatMessage);

            // Send updated unread count to receiver
            var unreadCount = await _context.ChatMessages
                .Where(m => m.ReceiverId == receiverId && !m.IsRead)
                .CountAsync();

            await Clients.Group(receiverId.ToString())
                         .SendAsync("UpdateUnreadCount", unreadCount, senderId);
        }

        public async Task MarkMessagesAsRead(int senderId, int receiverId)
        {
            var messages = await _context.ChatMessages
                .Where(m => m.SenderId == senderId && m.ReceiverId == receiverId && !m.IsRead)
                .ToListAsync();

            messages.ForEach(m => m.IsRead = true);
            await _context.SaveChangesAsync();

            // Notify receiver (optional, to update their badge if needed)
            var unreadCount = await _context.ChatMessages
                .Where(m => m.ReceiverId == receiverId && !m.IsRead)
                .CountAsync();

            await Clients.Group(receiverId.ToString())
                         .SendAsync("UpdateUnreadCount", unreadCount, senderId);
        }


        // ----------------------------
        // Load conversation history
        // ----------------------------
        public async Task LoadConversation(int userId, int partnerId)
        {
            var messages = await _context.ChatMessages
                .Where(m =>
                    (m.SenderId == userId && m.ReceiverId == partnerId) ||
                    (m.SenderId == partnerId && m.ReceiverId == userId))
                .OrderBy(m => m.SentAt)
                .Take(50) // optional limit
                .ToListAsync();

            await Clients.Caller.SendAsync("LoadChatHistory", messages);
        }

        // ----------------------------
        // Handle user connections
        // ----------------------------
        public override async Task OnConnectedAsync()
        {
            var userId = Context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                // Put user into a private group = their UserId
                await Groups.AddToGroupAsync(Context.ConnectionId, userId);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var userId = Context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
