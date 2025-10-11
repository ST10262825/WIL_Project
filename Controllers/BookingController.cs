using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TutorConnectAPI.Data;
using TutorConnectAPI.DTOs;
using TutorConnectAPI.Models;
using TutorConnectAPI.Services; // Add this for IGamificationService

namespace TutorConnectAPI.Controllers
{
    [ApiController]
    [Route("api/bookings")]
    public class BookingController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IGamificationService _gamificationService; // Add this

        // Update constructor to include gamification service
        public BookingController(ApplicationDbContext context, IGamificationService gamificationService)
        {
            _context = context;
            _gamificationService = gamificationService;
        }

        // ✅ Create booking with time slots (NO CHANGES NEEDED HERE)
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

        // ✅ Get all bookings for a student (NO CHANGES NEEDED HERE)
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

        // ✅ Get all bookings for a tutor (NO CHANGES NEEDED HERE)
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

        // ✅ UPDATE THIS METHOD: Update booking status with gamification
        [HttpPut("update-status/{bookingId}")]
        public async Task<IActionResult> UpdateBookingStatus(int bookingId, [FromQuery] string status)
        {
            try
            {
                var booking = await _context.Bookings
                    .Include(b => b.Student)
                    .ThenInclude(s => s.User)
                    .Include(b => b.Tutor)
                    .ThenInclude(t => t.User)
                    .FirstOrDefaultAsync(b => b.BookingId == bookingId);

                if (booking == null)
                    return NotFound("Booking not found.");

                var oldStatus = booking.Status;
                booking.Status = status;

                // If session is being marked as completed, trigger gamification
                if (status == "Completed" && oldStatus != "Completed")
                {
                    booking.CompletedAt = DateTime.UtcNow;

                    // Award gamification points to both student and tutor
                    if (booking.Student?.User != null)
                    {
                        await _gamificationService.AwardPointsAsync(
                            booking.Student.User.UserId,
                            "SessionCompleted",
                            50,
                            "Completed a tutoring session"
                        );
                    }

                    if (booking.Tutor?.User != null)
                    {
                        await _gamificationService.AwardPointsAsync(
                            booking.Tutor.User.UserId,
                            "SessionCompleted",
                            50,
                            "Completed a tutoring session"
                        );
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Message = $"Booking status updated to {status}",
                    BookingId = bookingId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error updating booking status: {ex.Message}");
            }
        }

        // ✅ ADD THIS NEW METHOD: Complete session with gamification
        [HttpPut("complete-session/{bookingId}")]
        public async Task<IActionResult> CompleteSession(int bookingId)
        {
            try
            {
                var booking = await _context.Bookings
                    .Include(b => b.Student)
                    .ThenInclude(s => s.User)
                    .Include(b => b.Tutor)
                    .ThenInclude(t => t.User)
                    .FirstOrDefaultAsync(b => b.BookingId == bookingId);

                if (booking == null)
                    return NotFound("Booking not found");

                // Update booking status
                booking.Status = "Completed";
                booking.CompletedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Award gamification points to both student and tutor
                if (booking.Student?.User != null)
                {
                    var studentResponse = await _gamificationService.AwardPointsAsync(
                        booking.Student.User.UserId,
                        "SessionCompleted",
                        50,
                        "Completed a tutoring session"
                    );
                }

                if (booking.Tutor?.User != null)
                {
                    var tutorResponse = await _gamificationService.AwardPointsAsync(
                        booking.Tutor.User.UserId,
                        "SessionCompleted",
                        50,
                        "Completed a tutoring session"
                    );
                }

                return Ok(new
                {
                    Message = "Session completed successfully",
                    BookingId = bookingId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error completing session: {ex.Message}");
            }
        }

        // ✅ Get available time slots for a tutor on a given day (NO CHANGES NEEDED HERE)
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