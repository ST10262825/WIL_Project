//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.SignalR;
//using Microsoft.EntityFrameworkCore;
//using System.Security.Claims;
//using TutorConnectAPI.Data;
//using TutorConnectAPI.DTOs;
//using TutorConnectAPI.Hubs;
//using TutorConnectAPI.Models;

//namespace TutorConnectAPI.Controllers
//{
//    [ApiController]
//    [Route("api/messages")]
//    [Authorize]
//    public class MessagesController : ControllerBase
//    {
//        private readonly ApplicationDbContext _context;
//        private readonly IHubContext<ChatHub> _hub;

//        public MessagesController(ApplicationDbContext context, IHubContext<ChatHub> hub)
//        {
//            _context = context;
//            _hub = hub;
//        }

//        // Get UserId from JWT
//        private int GetUserId() =>
//            int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new Exception("UserId claim missing"));

//        // Get chat history between current user and another user
//        [HttpGet("history/{otherUserId}")]
//        public async Task<IActionResult> GetHistory(int otherUserId)
//        {
//            var userId = GetUserId();

//            var messages = await _context.ChatMessages
//                .Where(m => (m.SenderId == userId && m.ReceiverId == otherUserId) ||
//                            (m.SenderId == otherUserId && m.ReceiverId == userId))
//                .OrderBy(m => m.SentAt)
//                .Select(m => new MessageDTO
//                {
//                    SenderId = m.SenderId,
//                    ReceiverId = m.ReceiverId,
//                    Content = m.Message,
//                    SentAt = m.SentAt
//                })
//                .ToListAsync();

//            return Ok(messages);
//        }

//        // Send a message
//        [HttpPost("send")]
//        public async Task<IActionResult> SendMessage(CreateMessageDTO dto)
//        {
//            var senderId = GetUserId();

//            var message = new ChatMessage
//            {
//                SenderId = senderId,
//                ReceiverId = dto.ReceiverId,
//                Message = dto.Content,
//                SentAt = DateTime.UtcNow
//            };

//            _context.ChatMessages.Add(message);
//            await _context.SaveChangesAsync();

//            // Send via SignalR to the receiver
//            await _hub.Clients.User(dto.ReceiverId.ToString())
//                .SendAsync("ReceiveMessage", senderId, dto.Content, message.SentAt);

//            return Ok(new MessageDTO
//            {
//                SenderId = senderId,
//                ReceiverId = dto.ReceiverId,
//                Content = dto.Content,
//                SentAt = message.SentAt
//            });
//        }
//    }
//}
