
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TutorConnectAPI.Data;
using TutorConnectAPI.Models;
using TutorConnectAPI.DTOs;

namespace TutorConnectAPI.Controllers
{
    [ApiController]
    [Route("api/student-dashboard")]
    public class StudentDashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public StudentDashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/student-dashboard/tutors
        [HttpGet("tutors")]
        public async Task<IActionResult> BrowseTutors()
        {
            var tutors = await _context.Tutors
                .Include(t => t.User)
                .Include(t => t.TutorModules)
                    .ThenInclude(tm => tm.Module)
                .Where(t => !t.IsBlocked && t.User.IsEmailVerified)
                .Select(t => new {
                    t.Id,
                    t.Name,
                    t.Surname,
                    t.Bio,
                    Modules = t.TutorModules.Select(tm => new {
                        tm.Module.Id,
                        tm.Module.Code,
                        tm.Module.Name
                    })
                })
                .ToListAsync();

            return Ok(tutors);
        }

        // GET: api/student-dashboard/tutors/{tutorId}/availability
        [HttpGet("tutors/{tutorId}/availability")]
        public async Task<IActionResult> GetTutorAvailability(int tutorId)
        {
            var now = DateTime.UtcNow;

            var bookedSlots = await _context.Sessions
                .Where(s => s.TutorId == tutorId && s.StartTime > now)
                .Select(s => new {
                    s.StartTime,
                    s.EndTime
                }).ToListAsync();

            return Ok(new
            {
                TutorId = tutorId,
                BookedSlots = bookedSlots
            });
        }

        // POST: api/student-dashboard/book-session
        [HttpPost("book-session")]
        public async Task<IActionResult> BookSession(SessionBookingDTO dto)
        {
            var session = new Session
            {
                TutorId = dto.TutorId,
                StudentId = dto.StudentId,
                ModuleId = dto.ModuleId,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime
            };

            _context.Sessions.Add(session);
            await _context.SaveChangesAsync();

            return Ok("Session booked successfully.");
        }

        // GET: api/student-dashboard/by-user/{userId}
        [HttpGet("by-user/{userId}")]
        public async Task<IActionResult> GetStudentByUserId(int userId)
        {
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (student == null) return NotFound();

            return Ok(new
            {
                student.Id,
                student.Name,
                student.Course
            });
        }

        // GET: api/student-dashboard/sessions/{studentId}
        // GET: api/student-dashboard/sessions/{studentId}
        [HttpGet("sessions/{studentId}")]
        public async Task<IActionResult> GetStudentSessions(int studentId)
        {
            var sessions = await _context.Sessions
                .Where(s => s.StudentId == studentId)
                .Include(s => s.Tutor)
                .ThenInclude(t => t.User)
                .Include(s => s.Module)
                .Select(s => new StudentSessionDTO
                {
                    Id = s.Id,
                    TutorName = s.Tutor.Name + " " + s.Tutor.Surname,
                    ModuleName = s.Module.Name,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    Status = s.Status.ToString()
                })
                .ToListAsync();

            return Ok(sessions);
        }



    }
}