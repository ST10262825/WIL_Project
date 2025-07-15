using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TutorConnectAPI.Data;
using TutorConnectAPI.Models;

namespace TutorConnectAPI.Controllers
{
    [ApiController]
    [Route("api/tutor-dashboard")]
    public class TutorDashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TutorDashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/tutor-dashboard/{tutorId}/modules
        [HttpGet("{tutorId}/modules")]
        public async Task<IActionResult> GetAssignedModules(int tutorId)
        {
            var modules = await _context.TutorModules
                .Where(tm => tm.TutorId == tutorId)
                .Include(tm => tm.Module)
                .Select(tm => new {
                    tm.Module.Id,
                    tm.Module.Code,
                    tm.Module.Name
                })
                .ToListAsync();

            return Ok(modules);
        }

        // PUT: api/tutor-dashboard/{tutorId}/bio
        [HttpPut("{tutorId}/bio")]
        public async Task<IActionResult> UpdateTutorBio(int tutorId, [FromBody] string newBio)
        {
            var tutor = await _context.Tutors.FindAsync(tutorId);
            if (tutor == null) return NotFound("Tutor not found.");

            tutor.Bio = newBio;
            await _context.SaveChangesAsync();

            return Ok("Bio updated successfully.");
        }

        // GET: api/tutor-dashboard/{tutorId}/sessions
        [HttpGet("{tutorId}/sessions")]
        public async Task<IActionResult> GetUpcomingSessions(int tutorId)
        {
            var sessions = await _context.Sessions
                .Where(s => s.TutorId == tutorId && s.StartTime > DateTime.UtcNow)
                .Include(s => s.Student)
                .Include(s => s.Module)
                .OrderBy(s => s.StartTime)
                .Select(s => new {
                    s.Id,
                    s.StartTime,
                    s.EndTime,
                    Status = s.Status.ToString(),
                    s.RejectionReason,
                    StudentName = s.Student.Name,
                    Module = s.Module.Name
                })
                .ToListAsync();

            return Ok(sessions);
        }


        // PUT: api/tutor-dashboard/sessions/{sessionId}/approve
        [HttpPut("sessions/{sessionId}/approve")]
        public async Task<IActionResult> ApproveSession(int sessionId)
        {
            var session = await _context.Sessions.FindAsync(sessionId);
            if (session == null) return NotFound("Session not found.");

            session.Status = SessionStatus.Approved;
            session.RejectionReason = null;

            await _context.SaveChangesAsync();
            return Ok("Session approved.");
        }

        // PUT: api/tutor-dashboard/sessions/{sessionId}/reject
        [HttpPut("sessions/{sessionId}/reject")]
        public async Task<IActionResult> RejectSession(int sessionId, [FromBody] string reason)
        {
            var session = await _context.Sessions.FindAsync(sessionId);
            if (session == null) return NotFound("Session not found.");

            session.Status = SessionStatus.Rejected;
            session.RejectionReason = reason;

            await _context.SaveChangesAsync();
            return Ok("Session rejected.");
        }

        [HttpGet("{tutorId}/sessions/past")]
        public async Task<IActionResult> GetPastSessions(int tutorId)
        {
            var pastSessions = await _context.Sessions
                .Where(s => s.TutorId == tutorId && s.EndTime <= DateTime.UtcNow)
                .Include(s => s.Student)
                .Include(s => s.Module)
                .OrderByDescending(s => s.EndTime)
                .Select(s => new {
                    s.Id,
                    s.StartTime,
                    s.EndTime,
                    Status = s.Status.ToString(),
                    StudentName = s.Student.Name,
                    Module = s.Module.Name
                })
                .ToListAsync();

            return Ok(pastSessions);
        }

        public class CompleteSessionDTO
        {
            public string? Feedback { get; set; }
        }

        [HttpPut("sessions/{sessionId}/complete")]
        public async Task<IActionResult> CompleteSession(int sessionId, [FromBody] CompleteSessionDTO dto)
        {
            var session = await _context.Sessions.FindAsync(sessionId);
            if (session == null) return NotFound("Session not found.");

            session.Status = SessionStatus.Completed;
            session.TutorFeedback = dto.Feedback;

            await _context.SaveChangesAsync();
            return Ok("Session marked as completed.");
        }

        [HttpGet("{tutorId}/session-summary")]
        public async Task<IActionResult> GetTutorSessionSummary(int tutorId)
        {
            var sessions = await _context.Sessions
                .Where(s => s.TutorId == tutorId)
                .ToListAsync();

            var summary = new
            {
                Total = sessions.Count,
                Approved = sessions.Count(s => s.Status == SessionStatus.Approved),
                Pending = sessions.Count(s => s.Status == SessionStatus.Pending),
                Rejected = sessions.Count(s => s.Status == SessionStatus.Rejected),
                Completed = sessions.Count(s => s.Status == SessionStatus.Completed)
            };

            return Ok(summary);
        }

        // GET: api/tutor-dashboard/user/{userId}
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetTutorByUserId(int userId)
        {
            var tutor = await _context.Tutors
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.UserId == userId);

            if (tutor == null)
                return NotFound($"No tutor found for userId = {userId}");

            return Ok(new
            {
                tutor.Id,
                tutor.UserId,
                tutor.Name,
                tutor.Surname,
                tutor.Phone,
                tutor.Bio,
                tutor.IsBlocked
            });
        }


    }
}
