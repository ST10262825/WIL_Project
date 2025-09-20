using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TutorConnectAPI.Data;
using TutorConnectAPI.DTOs;
using TutorConnectAPI.Models;

namespace TutorConnectAPI.Controllers
{
    [ApiController]
    [Route("api/tutor-dashboard")]
    public class TutorController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public TutorController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // ✅ Get tutor by UserId (for WebApp login)
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetTutorByUserId(int userId)
        {
            var tutor = await _context.Tutors
                .Include(t => t.User)
                .Include(t => t.TutorModules)
                    .ThenInclude(tm => tm.Module)
                .FirstOrDefaultAsync(t => t.UserId == userId);

            if (tutor == null)
                return NotFound("Tutor not found for this UserId.");

            var dto = new TutorDTO
            {
                TutorId = tutor.TutorId,
                UserId = tutor.UserId,
                Name = tutor.Name,
                Surname = tutor.Surname,
                Phone = tutor.Phone,
                Bio = tutor.Bio,
                AboutMe = tutor.AboutMe,
                Expertise = tutor.Expertise,
                Education = tutor.Education,
                IsBlocked = tutor.IsBlocked,
                ProfileImageUrl = tutor.ProfileImageUrl,
                Modules = tutor.TutorModules.Select(tm => new ModuleDTO
                {
                    Id = tm.Module.ModuleId,
                    Code = tm.Module.Code,
                    Name = tm.Module.Name
                }).ToList()
            };

            return Ok(dto);
        }

        [HttpPut("{tutorId}/profile")]
        public async Task<IActionResult> UpdateProfile(int tutorId, [FromForm] TutorProfileUpdateDto dto)
        {
            Console.WriteLine($"[DEBUG] UpdateProfile called for tutorId: {tutorId}");

            var tutor = await _context.Tutors.FindAsync(tutorId);
            if (tutor == null)
            {
                Console.WriteLine($"[DEBUG] Tutor with ID {tutorId} not found.");
                return NotFound("Tutor not found");
            }

            
            tutor.Bio = dto.Bio;
            tutor.AboutMe = dto.AboutMe;
            tutor.Expertise = dto.Expertise;
            tutor.Education = dto.Education;

            // Update profile image
            if (dto.ProfileImage != null)
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath, "images");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{Guid.NewGuid()}_{dto.ProfileImage.FileName}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await dto.ProfileImage.CopyToAsync(stream);

                // 🔥 Build absolute URL
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                tutor.ProfileImageUrl = $"{baseUrl}/images/{fileName}";
            }

            await _context.SaveChangesAsync();

            // ✅ Return updated profile
            return Ok(new
            {
                tutor.Bio,
                tutor.AboutMe,
                tutor.Expertise,
                tutor.Education,
                tutor.ProfileImageUrl
            });
        }








        // ✅ Get all bookings for this tutor
        //[HttpGet("sessions/{tutorId}")]
        //public async Task<IActionResult> GetTutorBookings(int tutorId)
        //{
        //    var bookings = await _context.Bookings
        //        .Include(b => b.Student)
        //            .ThenInclude(s => s.User)
        //        .Include(b => b.Module)
        //        .Where(b => b.TutorId == tutorId)
        //        .Select(b => new BookingDTO
        //        {
        //            BookingId = b.BookingId,
        //            TutorId = b.TutorId,
        //            TutorName = b.Tutor.Name + " " + b.Tutor.Surname,
        //            StudentId = b.StudentId,
        //            StudentName = b.Student.Name,
        //            ModuleName = b.Module.Name,
        //            SessionDate = b.SessionDate,
        //            Status = b.Status,
        //            Notes = b.Notes
        //        })
        //        .ToListAsync();

        //    return Ok(bookings);
        //}

        //// ✅ Update booking/session status
        //[HttpPut("sessions/{sessionId}/status")]
        //public async Task<IActionResult> UpdateSessionStatus(int sessionId, [FromBody] UpdateSessionStatusDTO dto)
        //{
        //    var booking = await _context.Bookings.FindAsync(sessionId);
        //    if (booking == null)
        //        return NotFound("Booking not found.");

        //    booking.Status = dto.Status;
        //    booking.Notes = dto.RejectionReason; // optional
        //    await _context.SaveChangesAsync();

        //    return Ok("Booking status updated.");
        //}

        //[HttpGet("stats/{tutorId}")]
        //public async Task<IActionResult> GetDashboardStats(int tutorId)
        //{
        //    var tutor = await _context.Tutors.FindAsync(tutorId);
        //    if (tutor == null) return NotFound("Tutor not found.");

        //    var today = DateTime.Today;

        //    var stats = new
        //    {
        //        ActiveSessionsCount = await _context.Bookings.CountAsync(b => b.TutorId == tutorId && b.SessionDate >= today),
        //        TotalStudentsCount = await _context.Bookings.Where(b => b.TutorId == tutorId).Select(b => b.StudentId).Distinct().CountAsync(),
        //        PendingBookingsCount = await _context.Bookings.CountAsync(b => b.TutorId == tutorId && b.Status == "Pending"),
        //        CompletedSessionsCount = await _context.Bookings.CountAsync(b => b.TutorId == tutorId && b.Status == "Completed"),
        //        UpcomingSessions = await _context.Bookings
        //            .Where(b => b.TutorId == tutorId && b.SessionDate >= today)
        //            .OrderBy(b => b.SessionDate)
        //            .Select(b => new
        //            {
        //                b.BookingId,
        //                b.StudentId,
        //                StudentName = b.Student.Name,
        //                ModuleName = b.Module.Name,
        //                b.SessionDate,
        //                b.Status
        //            }).ToListAsync(),
        //        //UnreadMessagesCount = await _context.ChatMessages.CountAsync(m => m.ReceiverId == tutor.UserId && !m.IsRead)
        //    };

        //    return Ok(stats);
        //}

        [HttpGet("browse")]
        public async Task<IActionResult> BrowseTutors()
        {
            var tutors = await _context.Tutors
                .Include(t => t.TutorModules)
                    .ThenInclude(tm => tm.Module)
                .ToListAsync();

            var dtoList = tutors.Select(t => new BrowseTutorDTO
            {
                TutorId = t.TutorId,
                FullName = $"{t.Name} {t.Surname}",
                ProfileImageUrl = string.IsNullOrEmpty(t.ProfileImageUrl) ? "/images/default-profile.png" : t.ProfileImageUrl,
                AboutMe = t.AboutMe,
                Expertise = t.Expertise,
                Education = t.Education,
                Subjects = t.TutorModules.Select(tm => tm.Module.Name).ToList(),
                IsVerified = true // or implement logic
            }).ToList();

            return Ok(dtoList);
        }

        [HttpGet("by-id/{tutorId}")]
        public async Task<IActionResult> GetTutorById(int tutorId)
        {
            var tutor = await _context.Tutors
                .Include(t => t.TutorModules)
                    .ThenInclude(tm => tm.Module)
                .FirstOrDefaultAsync(t => t.TutorId == tutorId);

            if (tutor == null)
                return NotFound("Tutor not found.");

            var dto = new TutorDTO
            {
                TutorId = tutor.TutorId,
                UserId = tutor.UserId,
                Name = tutor.Name,
                Surname = tutor.Surname,
                Phone = tutor.Phone,
                Bio = tutor.Bio,
                AboutMe = tutor.AboutMe,
                Expertise = tutor.Expertise,
                Education = tutor.Education,
                IsBlocked = tutor.IsBlocked,
                ProfileImageUrl = string.IsNullOrEmpty(tutor.ProfileImageUrl) ? "/images/default-profile.png" : tutor.ProfileImageUrl,
                Modules = tutor.TutorModules.Select(tm => new ModuleDTO
                {
                    Id = tm.Module.ModuleId,
                    Code = tm.Module.Code,
                    Name = tm.Module.Name
                }).ToList()
            };

            return Ok(dto);
        }


    }
}
