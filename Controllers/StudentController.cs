using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TutorConnectAPI.Data;
using TutorConnectAPI.DTOs;
using TutorConnectAPI.Models;

namespace TutorConnectAPI.Controllers
{
    [ApiController]
    [Route("api/student")]
    [Authorize(Roles = "Student")]
    public class StudentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public StudentController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Helper: Get UserId from JWT claims
        private int? GetUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(idClaim, out var userId))
                return userId;

            return null;
        }

        // Dashboard summary endpoint
        [HttpGet("dashboard-summary")]
        public async Task<IActionResult> GetDashboardSummary()
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized("UserId claim missing. Please re-login.");

            var student = await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.UserId == userId.Value);

            if (student == null)
                return NotFound("No student profile found for this user");

            var availableTutors = await _context.Tutors.CountAsync(t => !t.IsBlocked);

            // TODO: Replace with actual upcoming bookings count
            var upcomingBookings = await _context.Bookings
                .CountAsync(b => b.StudentId == student.StudentId && b.Status == "Upcoming");

            var dto = new StudentDashboardSummaryDTO
            {
                StudentName = student.Name,
                AvailableTutors = availableTutors,
                UpcomingBookings = upcomingBookings
            };

            return Ok(dto);
        }

        // Get student by UserId (for WebApp)
        [HttpGet("by-user/{userId}")]
        public async Task<IActionResult> GetStudentByUserId(int userId)
        {
            var student = await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (student == null)
                return NotFound("No student profile found for this user");

            var dto = new StudentDTO
            {
                StudentId = student.StudentId,
                UserId = student.UserId,
                Name = student.Name,
                Course = student.Course
            };

            return Ok(dto);
        }
    }
}