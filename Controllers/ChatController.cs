using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
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
        try
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

            Console.WriteLine($"=== CHAT DEBUG ===");
            Console.WriteLine($"Current User ID: {currentUserId}");
            Console.WriteLine($"Current User Role: {currentUserRole}");

            // Step 1: Get the current user's TutorId or StudentId
            int? currentTutorId = null;
            int? currentStudentId = null;

            if (currentUserRole == "Tutor")
            {
                var tutor = await _context.Tutors.FirstOrDefaultAsync(t => t.UserId == currentUserId);
                currentTutorId = tutor?.TutorId;
                Console.WriteLine($"Current Tutor ID: {currentTutorId}");
            }
            else if (currentUserRole == "Student")
            {
                var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == currentUserId);
                currentStudentId = student?.StudentId;
                Console.WriteLine($"Current Student ID: {currentStudentId}");
            }

            if (currentTutorId == null && currentStudentId == null)
            {
                Console.WriteLine("Could not find Tutor or Student record for current user");
                return Ok(new List<ChatUserDTO>());
            }

            // Step 2: Get bookings for the current user
            var bookingUserIds = new List<int>();

            if (currentUserRole == "Student" && currentStudentId.HasValue)
            {
                // Student: Get all tutors they have bookings with
                var tutorIds = await _context.Bookings
                    .Where(b => b.StudentId == currentStudentId.Value && b.Status != "Declined")
                    .Select(b => b.TutorId)
                    .Distinct()
                    .ToListAsync();

                Console.WriteLine($"Tutor IDs from bookings: {string.Join(", ", tutorIds)}");

                // Convert TutorId to UserId
                foreach (var tutorId in tutorIds)
                {
                    var tutor = await _context.Tutors
                        .Include(t => t.User)
                        .FirstOrDefaultAsync(t => t.TutorId == tutorId && t.User.IsActive);

                    if (tutor?.User != null)
                    {
                        bookingUserIds.Add(tutor.User.UserId);
                        Console.WriteLine($"Tutor {tutorId} -> User {tutor.User.UserId}");
                    }
                }
            }
            else if (currentUserRole == "Tutor" && currentTutorId.HasValue)
            {
                // Tutor: Get all students they have bookings with
                var studentIds = await _context.Bookings
                    .Where(b => b.TutorId == currentTutorId.Value && b.Status != "Declined")
                    .Select(b => b.StudentId)
                    .Distinct()
                    .ToListAsync();

                Console.WriteLine($"Student IDs from bookings: {string.Join(", ", studentIds)}");

                // Convert StudentId to UserId
                foreach (var studentId in studentIds)
                {
                    var student = await _context.Students
                        .Include(s => s.User)
                        .FirstOrDefaultAsync(s => s.StudentId == studentId && s.User.IsActive);

                    if (student?.User != null)
                    {
                        bookingUserIds.Add(student.User.UserId);
                        Console.WriteLine($"Student {studentId} -> User {student.User.UserId}");
                    }
                }
            }

            Console.WriteLine($"Final User IDs for chat: {string.Join(", ", bookingUserIds)}");

            if (!bookingUserIds.Any())
            {
                Console.WriteLine("No valid users found for chat");
                return Ok(new List<ChatUserDTO>());
            }

            // Step 3: Build the chat user list
            var users = new List<ChatUserDTO>();
            foreach (var userId in bookingUserIds.Distinct())
            {
                var user = await _context.Users
                    .Include(u => u.Tutor)
                    .Include(u => u.Student)
                    .FirstOrDefaultAsync(u => u.UserId == userId && u.IsActive);

                if (user == null) continue;

                string name = "";
                if (user.Role == "Tutor" && user.Tutor != null)
                {
                    name = $"{user.Tutor.Name} {user.Tutor.Surname}";
                }
                else if (user.Role == "Student" && user.Student != null)
                {
                    name = user.Student.Name;
                }
                else
                {
                    continue; // Skip if we can't get the name
                }

                var unreadCount = await _context.ChatMessages
                    .CountAsync(m => m.SenderId == user.UserId && m.ReceiverId == currentUserId && !m.IsRead);

                var lastMessage = await _context.ChatMessages
                    .Where(m => (m.SenderId == user.UserId && m.ReceiverId == currentUserId) ||
                               (m.SenderId == currentUserId && m.ReceiverId == user.UserId))
                    .OrderByDescending(m => m.SentAt)
                    .FirstOrDefaultAsync();

                users.Add(new ChatUserDTO
                {
                    UserId = user.UserId,
                    Name = name,
                    Role = user.Role,
                    Email = user.Email,
                    UnreadCount = unreadCount,
                    LastMessageTime = lastMessage?.SentAt,
                    LastMessagePreview = lastMessage?.Message ?? "No messages yet"
                });

                Console.WriteLine($"Added user: {name} ({user.Role}) - UserId: {user.UserId}");
            }

            Console.WriteLine($"Final users to return: {users.Count}");
            Console.WriteLine($"=== END CHAT DEBUG ===");

            // Sort by last message time (most recent first), then by name
            var sortedUsers = users
                .OrderByDescending(u => u.LastMessageTime ?? DateTime.MinValue)
                .ThenBy(u => u.Name)
                .ToList();

            return Ok(sortedUsers);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"=== CHAT ERROR ===");
            Console.WriteLine($"Error in GetUsers: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            Console.WriteLine($"=== END CHAT ERROR ===");
            return StatusCode(500, new { error = "Internal server error" });
        }
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

    [HttpGet("contacts")]
    [Authorize]
    public async Task<IActionResult> GetChatContacts()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

        // Get bookings where user is involved
        var bookings = await _context.Bookings
            .Where(b => b.StudentId == userId || b.TutorId == userId)
            .ToListAsync();

        var contacts = bookings
            .Select(b => b.StudentId == userId
                         ? new { UserId = b.TutorId, Name = b.Tutor.Name }
                         : new { UserId = b.StudentId, Name = b.Student.Name })
            .Distinct()
            .ToList();

        return Ok(contacts);
    }

    [HttpGet("debug-database")]
    public async Task<IActionResult> DebugDatabase()
    {
        try
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

            // Get current user's Tutor/Student ID
            int? currentTutorId = null;
            int? currentStudentId = null;
            string currentUserName = "";

            if (currentUserRole == "Tutor")
            {
                var tutor = await _context.Tutors.FirstOrDefaultAsync(t => t.UserId == currentUserId);
                currentTutorId = tutor?.TutorId;
                currentUserName = tutor != null ? $"{tutor.Name} {tutor.Surname}" : "Unknown Tutor";
            }
            else if (currentUserRole == "Student")
            {
                var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == currentUserId);
                currentStudentId = student?.StudentId;
                currentUserName = student?.Name ?? "Unknown Student";
            }

            var results = new
            {
                CurrentUser = new
                {
                    UserId = currentUserId,
                    Role = currentUserRole,
                    Name = currentUserName,
                    TutorId = currentTutorId,
                    StudentId = currentStudentId
                },
                TotalUsers = await _context.Users.CountAsync(),
                TotalTutors = await _context.Tutors.CountAsync(),
                TotalStudents = await _context.Students.CountAsync(),
                TotalBookings = await _context.Bookings.CountAsync(),
                MyBookings = currentUserRole == "Student" && currentStudentId.HasValue
                    ? await _context.Bookings.CountAsync(b => b.StudentId == currentStudentId.Value)
                    : currentUserRole == "Tutor" && currentTutorId.HasValue
                    ? await _context.Bookings.CountAsync(b => b.TutorId == currentTutorId.Value)
                    : 0,
                SampleBookings = await _context.Bookings
                    .Take(5)
                    .Select(b => new { b.BookingId, b.StudentId, b.TutorId, b.Status })
                    .ToListAsync(),
                AllTutors = await _context.Tutors
                    .Select(t => new { t.TutorId, t.UserId, t.Name, t.Surname })
                    .ToListAsync(),
                AllStudents = await _context.Students
                    .Select(s => new { s.StudentId, s.UserId, s.Name })
                    .ToListAsync()
            };

            return Ok(results);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }


}
