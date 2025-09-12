using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TutorConnectAPI.Data;
using TutorConnectAPI.DTOs;
using TutorConnectAPI.Models;

namespace TutorConnectAPI.Controllers
{
    [ApiController]
    [Route("api/bookings")]
    public class BookingController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public BookingController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateBooking(CreateBookingDTO dto)
        {
            var booking = new Booking
            {
                TutorId = dto.TutorId,
                StudentId = dto.StudentId,
                ModuleId = dto.ModuleId,
                SessionDate = dto.SessionDate,
                Notes = dto.Notes,
                Status = "Pending"
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Booking created successfully." });
        }

        [HttpGet("student/{studentId}")]
        public async Task<IActionResult> GetStudentBookings(int studentId)
        {
            var bookings = await _context.Bookings
                .Include(b => b.Tutor)
                .ThenInclude(t => t.User)
                .Include(b => b.Module)
                .Where(b => b.StudentId == studentId)
                .Select(b => new BookingDTO
                {
                    BookingId = b.BookingId,
                    TutorId = b.TutorId,
                    TutorName = b.Tutor.Name + " " + b.Tutor.Surname,
                    StudentId = b.StudentId,
                    StudentName = b.Student.Name,
                    ModuleName = b.Module.Name,
                    SessionDate = b.SessionDate,
                    Status = b.Status,
                    Notes = b.Notes
                })
                .ToListAsync();

            return Ok(bookings);
        }

        [HttpPut("update-status/{bookingId}")]
        public async Task<IActionResult> UpdateBookingStatus(int bookingId, [FromQuery] string status)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking == null)
                return NotFound("Booking not found.");

            booking.Status = status;
            await _context.SaveChangesAsync();

            return Ok("Booking status updated.");
        }

        [HttpGet("tutor/{tutorId}")]
        public async Task<IActionResult> GetTutorBookings(int tutorId)
        {
            var bookings = await _context.Bookings
                .Include(b => b.Student)
                .ThenInclude(s => s.User)
                .Include(b => b.Module)
                .Where(b => b.TutorId == tutorId)
                .Select(b => new BookingDTO
                {
                    BookingId = b.BookingId,
                    TutorId = b.TutorId,
                    TutorName = b.Tutor.Name + " " + b.Tutor.Surname,
                    StudentId = b.StudentId,
                    StudentName = b.Student.Name,
                    ModuleName = b.Module.Name,
                    SessionDate = b.SessionDate,
                    Status = b.Status,
                    Notes = b.Notes
                })
                .ToListAsync();

            return Ok(bookings);
        }


    }
}
