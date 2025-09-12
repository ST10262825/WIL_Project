using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TutorConnectAPI.Data;
using TutorConnectAPI.DTOs;
using TutorConnectAPI.Models;

[ApiController]
[Route("api/chat")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ChatController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);

        var users = await _context.Users
            .Where(u => u.UserId != currentUserId && u.IsActive)
            .Select(u => new ChatUserDTO
            {
                UserId = u.UserId,
                Name = _context.Tutors.Any(t => t.UserId == u.UserId)
                            ? _context.Tutors.FirstOrDefault(t => t.UserId == u.UserId).Name
                            : _context.Students.FirstOrDefault(s => s.UserId == u.UserId).Name
            })
            .ToListAsync();

        return Ok(users);
    }


    // Get unread messages count for a user
    [HttpGet("unread-count/{userId}")]
    public async Task<IActionResult> GetUnreadMessagesCount(int userId)
    {
        var count = await _context.ChatMessages
            .Where(m => m.ReceiverId == userId && !m.IsRead)
            .CountAsync();

        return Ok(count);
    }


    [HttpGet("messages/{receiverId}")]
    public async Task<IActionResult> GetMessages(int receiverId)
    {
        var currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);

        var messages = await _context.ChatMessages
            .Where(m => (m.SenderId == currentUserId && m.ReceiverId == receiverId)
                     || (m.SenderId == receiverId && m.ReceiverId == currentUserId))
            .OrderBy(m => m.SentAt)
            .Select(m => new
            {
                senderId = m.SenderId,
                senderName = m.SenderId == m.Sender.UserId && m.Sender.Role == "Student"
                    ? _context.Students.Where(s => s.UserId == m.SenderId).Select(s => s.Name).FirstOrDefault()
                    : _context.Tutors.Where(t => t.UserId == m.SenderId).Select(t => t.Name + " " + t.Surname).FirstOrDefault(),
                message = m.Message,
                sentAt = m.SentAt
            })
            .ToListAsync();

        return Ok(messages);
    }

    // Mark all messages from a specific sender as read for the current user
    // Marks all messages from a specific sender to current user as read
    [HttpPut("mark-read/{senderId}")]
    public async Task<IActionResult> MarkMessagesAsRead(int senderId)
    {
        var currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);

        var messages = await _context.ChatMessages
            .Where(m => m.SenderId == senderId && m.ReceiverId == currentUserId && !m.IsRead)
            .ToListAsync();

        messages.ForEach(m => m.IsRead = true);

        await _context.SaveChangesAsync();
        return Ok();
    }



}
