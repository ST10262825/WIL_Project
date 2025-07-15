using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TutorConnectAPI.Data;
using TutorConnectAPI.DTOs;
using TutorConnectAPI.Models;

namespace TutorConnectAPI.Controllers
{
    [ApiController]
    [Route("api/sessions")]
    public class SessionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SessionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // POST: api/sessions/book
        [HttpPost("book")]
        public async Task<IActionResult> BookSession(SessionBookingDTO dto)
        {
            var student = await _context.Students.FindAsync(dto.StudentId);
            var tutor = await _context.Tutors.FindAsync(dto.TutorId);
            var module = await _context.Modules.FindAsync(dto.ModuleId);

            if (student == null || tutor == null || module == null)
                return BadRequest("Invalid student, tutor, or module.");

            if (dto.EndTime <= dto.StartTime || dto.StartTime <= DateTime.UtcNow)
                return BadRequest("Invalid session time.");

            // Check for tutor availability
            bool isTutorBusy = await _context.Sessions.AnyAsync(s =>
                s.TutorId == dto.TutorId &&
                ((dto.StartTime >= s.StartTime && dto.StartTime < s.EndTime) ||
                 (dto.EndTime > s.StartTime && dto.EndTime <= s.EndTime)));

            if (isTutorBusy)
                return Conflict("Tutor is not available during this time.");

            var session = new Session
            {
                StudentId = dto.StudentId,
                TutorId = dto.TutorId,
                ModuleId = dto.ModuleId,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                Status = SessionStatus.Pending
            };

            _context.Sessions.Add(session);
            await _context.SaveChangesAsync();

            return Ok("Session booked successfully.");
        }

        // GET: api/sessions/student/5
        [HttpGet("student/{studentId}")]
        public async Task<IActionResult> GetStudentSessions(int studentId)
        {
            var sessions = await _context.Sessions
                .Where(s => s.StudentId == studentId)
                .Include(s => s.Tutor)
                .Include(s => s.Module)
                .OrderBy(s => s.StartTime)
                .Select(s => new
                {
                    s.Id,
                    s.StartTime,
                    s.EndTime,
                    s.Status,
                    s.RejectionReason,
                    TutorName = s.Tutor.Name,
                    ModuleName = s.Module.Name
                })
                .ToListAsync();

            return Ok(sessions);
        }

        // PUT: api/sessions/{sessionId}/approve
        [HttpPut("{sessionId}/approve")]
        public async Task<IActionResult> ApproveSession(int sessionId)
        {
            var session = await _context.Sessions.FindAsync(sessionId);
            if (session == null)
                return NotFound("Session not found.");

            if (session.Status != SessionStatus.Pending)
                return BadRequest("Only pending sessions can be approved.");

            session.Status = SessionStatus.Approved;
            await _context.SaveChangesAsync();

            return Ok("Session approved.");
        }

        // PUT: api/sessions/{sessionId}/reject
        [HttpPut("{sessionId}/reject")]
        public async Task<IActionResult> RejectSession(int sessionId, [FromBody] string reason)
        {
            var session = await _context.Sessions.FindAsync(sessionId);
            if (session == null)
                return NotFound("Session not found.");

            if (session.Status != SessionStatus.Pending)
                return BadRequest("Only pending sessions can be rejected.");

            session.Status = SessionStatus.Rejected;
            session.RejectionReason = reason;
            await _context.SaveChangesAsync();

            return Ok("Session rejected.");
        }

        // PUT: api/sessions/{sessionId}/complete
        [HttpPut("{sessionId}/complete")]
        public async Task<IActionResult> CompleteSession(int sessionId, [FromBody] string? feedback)
        {
            var session = await _context.Sessions.FindAsync(sessionId);
            if (session == null)
                return NotFound("Session not found.");

            if (session.Status != SessionStatus.Approved)
                return BadRequest("Only approved sessions can be completed.");

            session.Status = SessionStatus.Completed;
            session.TutorFeedback = feedback;
            await _context.SaveChangesAsync();

            return Ok("Session marked as completed.");
        }
    }
}
