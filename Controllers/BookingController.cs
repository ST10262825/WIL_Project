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

        // ✅ Create booking with time slots
        [HttpPost("create")]
        public async Task<IActionResult> CreateBooking(CreateBookingDTO dto)
        {
            try
            {
                // === ADD VALIDATION CHECKS ===
                // Check if student exists
                var studentExists = await _context.Students.AnyAsync(s => s.StudentId == dto.StudentId);
                if (!studentExists)
                {
                    return BadRequest($"Student with ID {dto.StudentId} does not exist.");
                }

                // Check if tutor exists
                var tutorExists = await _context.Tutors.AnyAsync(t => t.TutorId == dto.TutorId);
                if (!tutorExists)
                {
                    return BadRequest($"Tutor with ID {dto.TutorId} does not exist.");
                }

                // Check if module exists
                var moduleExists = await _context.Modules.AnyAsync(m => m.ModuleId == dto.ModuleId);
                if (!moduleExists)
                {
                    return BadRequest($"Module with ID {dto.ModuleId} does not exist.");
                }

                // Check if tutor is already booked for that time slot
                bool overlapping = await _context.Bookings.AnyAsync(b =>
                    b.TutorId == dto.TutorId &&
                    b.StartTime < dto.EndTime &&
                    b.EndTime > dto.StartTime &&
                    b.Status != "Declined" // Don't count declined bookings
                );

                if (overlapping)
                    return BadRequest("This time slot is already booked for the selected tutor.");

                var booking = new Booking
                {
                    TutorId = dto.TutorId,
                    StudentId = dto.StudentId,
                    ModuleId = dto.ModuleId,
                    StartTime = dto.StartTime,
                    EndTime = dto.EndTime,
                    Notes = dto.Notes,
                    Status = "Pending"
                };

                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Booking created successfully.", bookingId = booking.BookingId });
            }
            catch (DbUpdateException dbEx)
            {
                // Handle database-specific errors
                return StatusCode(500, $"Database error: {dbEx.InnerException?.Message}");
            }
            catch (Exception ex)
            {
                // Handle other errors
                return StatusCode(500, "An unexpected error occurred while creating the booking.");
            }
        }

        // ✅ Get all bookings for a student
        [HttpGet("student/{studentId}")]
        public async Task<IActionResult> GetStudentBookings(int studentId)
        {
            var bookings = await _context.Bookings
                .Include(b => b.Tutor).ThenInclude(t => t.User)
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
                    StartTime = b.StartTime,
                    EndTime = b.EndTime,
                    Status = b.Status,
                    Notes = b.Notes
                })
                .ToListAsync();

            return Ok(bookings);
        }

        // ✅ Get all bookings for a tutor
        [HttpGet("tutor/{tutorId}")]
        public async Task<IActionResult> GetTutorBookings(int tutorId)
        {
            var bookings = await _context.Bookings
                .Include(b => b.Student).ThenInclude(s => s.User)
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
                    StartTime = b.StartTime,
                    EndTime = b.EndTime,
                    Status = b.Status,
                    Notes = b.Notes
                })
                .ToListAsync();

            return Ok(bookings);
        }

        // ✅ Update booking status
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

        // ✅ Get available time slots for a tutor on a given day
        [HttpGet("tutor/{tutorId}/availability")]
        public async Task<IActionResult> GetTutorAvailability(int tutorId, [FromQuery] DateTime date)
        {
            try
            {
                // Assume working hours 9 AM - 5 PM
                var startOfDay = date.Date.AddHours(9);
                var endOfDay = date.Date.AddHours(17);

                // Get existing bookings for this tutor on the selected date
                var existingBookings = await _context.Bookings
                    .Where(b => b.TutorId == tutorId &&
                                b.StartTime.Date == date.Date &&
                                b.Status != "Declined") // Exclude declined bookings
                    .Select(b => new { b.StartTime, b.EndTime })
                    .ToListAsync();

                var timeSlots = new List<object>();

                // Generate hourly slots from 9 AM to 5 PM
                for (var time = startOfDay; time < endOfDay; time = time.AddHours(1))
                {
                    var slotStart = time;
                    var slotEnd = time.AddHours(1);

                    // Check if this slot overlaps with any existing booking
                    bool isBooked = existingBookings.Any(b =>
                        (b.StartTime < slotEnd && b.EndTime > slotStart));

                    timeSlots.Add(new
                    {
                        Start = slotStart,
                        End = slotEnd,
                        Available = !isBooked
                    });
                }

                return Ok(timeSlots);
            }
            catch (Exception ex)
            {
                // Log the exception here
                return StatusCode(500, "An error occurred while fetching availability");
            }
        }
    }
}
