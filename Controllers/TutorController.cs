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


        // ✅ Get tutor by TutorId (for public profile viewing)
        [HttpGet("by-id/{tutorId}")]
        public async Task<IActionResult> GetTutorById(int tutorId)
        {
            var tutor = await _context.Tutors
                .Include(t => t.User)
                .Include(t => t.Course)
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

                // ✅ ADD COURSE INFO
                CourseId = tutor.CourseId,
                CourseName = tutor.Course?.Title ?? "Not assigned",

                // ✅ ADD RATING PROPERTIES
                AverageRating = tutor.AverageRating,
                TotalReviews = tutor.TotalReviews,
                RatingCount1 = tutor.RatingCount1,
                RatingCount2 = tutor.RatingCount2,
                RatingCount3 = tutor.RatingCount3,
                RatingCount4 = tutor.RatingCount4,
                RatingCount5 = tutor.RatingCount5,

                Modules = tutor.TutorModules.Select(tm => new ModuleDTO
                {
                    ModuleId = tm.ModuleId,
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


        [HttpGet("browse")]
        public async Task<IActionResult> BrowseTutors()
        {
            var tutors = await _context.Tutors
                .Include(t => t.Course) // ADD THIS LINE - Include Course
                .Include(t => t.TutorModules)
                    .ThenInclude(tm => tm.Module)
                .Where(t => !t.IsBlocked) // ADD THIS - Exclude blocked tutors
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
                IsVerified = true,

                // ✅ ADD RATING PROPERTIES FOR BROWSE VIEW TOO
                AverageRating = t.AverageRating,
                TotalReviews = t.TotalReviews,

                // ✅ ADD COURSE PROPERTIES - THIS IS WHAT'S MISSING!
                CourseId = t.CourseId,
                CourseName = t.Course?.Title ?? "Not assigned" // Handle null course
            }).ToList();

            return Ok(dtoList);
        }



        [HttpGet("{tutorId}")]
            public async Task<IActionResult> GetTutorById(int tutorId, [FromQuery] int? studentId = null)
            {
                var tutor = await _context.Tutors
                    .Include(t => t.User)
                    .Include(t => t.Course)
                    .Include(t => t.TutorModules)
                        .ThenInclude(tm => tm.Module)
                    .FirstOrDefaultAsync(t => t.TutorId == tutorId);

                if (tutor == null)
                    return NotFound("Tutor not found.");

                // If studentId provided, verify course access
                if (studentId.HasValue)
                {
                    var student = await _context.Students
                        .FirstOrDefaultAsync(s => s.StudentId == studentId.Value);

                    if (student != null && student.CourseId != tutor.CourseId)
                    {
                        return Forbid("Student cannot access tutors from different courses.");
                    }
                }



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

                // ✅ ADD THESE RATING PROPERTIES
                AverageRating = tutor.AverageRating,
                TotalReviews = tutor.TotalReviews,
                RatingCount1 = tutor.RatingCount1,
                RatingCount2 = tutor.RatingCount2,
                RatingCount3 = tutor.RatingCount3,
                RatingCount4 = tutor.RatingCount4,
                RatingCount5 = tutor.RatingCount5,


                Modules = tutor.TutorModules.Select(tm => new ModuleDTO
                {
                    ModuleId = tm.ModuleId,
                    Code = tm.Module.Code,
                    Name = tm.Module.Name
                }).ToList()
            };

            return Ok(dto);
        }





        // ✅ Get tutor by UserId (for WebApp login/dashboard) - ADD THIS METHOD
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetTutorByUserId(int userId)
        {
            var tutor = await _context.Tutors
                .Include(t => t.User)
                .Include(t => t.Course)
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
                ProfileImageUrl = string.IsNullOrEmpty(tutor.ProfileImageUrl) ? "/images/default-profile.png" : tutor.ProfileImageUrl,

                // ✅ ADD COURSE INFO
                CourseId = tutor.CourseId,
                CourseName = tutor.Course?.Title ?? "Not assigned",

                // ✅ ADD RATING PROPERTIES
                AverageRating = tutor.AverageRating,
                TotalReviews = tutor.TotalReviews,
                RatingCount1 = tutor.RatingCount1,
                RatingCount2 = tutor.RatingCount2,
                RatingCount3 = tutor.RatingCount3,
                RatingCount4 = tutor.RatingCount4,
                RatingCount5 = tutor.RatingCount5,

                Modules = tutor.TutorModules.Select(tm => new ModuleDTO
                {
                    ModuleId = tm.ModuleId,
                    Code = tm.Module.Code,
                    Name = tm.Module.Name
                }).ToList()
            };

            return Ok(dto);
        }

        // In your API TutorController - Add this method for students
        [HttpGet("browse-for-student/{studentId}")]
        public async Task<IActionResult> GetTutorsForStudent(int studentId)
        {
            try
            {
                // Get student's course
                var student = await _context.Students
                    .Include(s => s.Course)
                    .FirstOrDefaultAsync(s => s.StudentId == studentId);

                if (student == null)
                    return NotFound("Student not found");

                // Only return tutors from the same course
                var tutors = await _context.Tutors
                    .Include(t => t.User)
                    .Include(t => t.Course)
                    .Include(t => t.TutorModules)
                        .ThenInclude(tm => tm.Module)
                    .Where(t => t.CourseId == student.CourseId && !t.IsBlocked) // IMPORTANT: Course filter
                    .Select(t => new BrowseTutorDTO
                    {
                        TutorId = t.TutorId,
                        FullName = $"{t.Name} {t.Surname}",
                        ProfileImageUrl = t.ProfileImageUrl,
                        AboutMe = t.AboutMe,
                        Expertise = t.Expertise,
                        Education = t.Education,
                        Subjects = t.TutorModules.Select(tm => tm.Module.Name).ToList(),
                        IsVerified = true,
                        AverageRating = t.AverageRating,
                        TotalReviews = t.TotalReviews,
                        CourseId = t.CourseId, // Ensure this is included
                         CourseName = t.Course.Title
                    })
                    .ToListAsync();

                return Ok(tutors);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error loading tutors: {ex.Message}");
            }
        }

        // Add to your TutorController
        [HttpPut("change-password")]
        public async Task<IActionResult> ChangeTutorPassword([FromBody] ChangePasswordDTO dto)
        {
            try
            {
                // Get tutor ID from route or user claims
                var tutorId = int.Parse(User.FindFirst("tutorId")?.Value);

                var tutor = await _context.Tutors
                    .Include(t => t.User)
                    .FirstOrDefaultAsync(t => t.TutorId == tutorId);

                if (tutor == null)
                    return NotFound("Tutor not found.");

                // Verify current password
                if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, tutor.User.PasswordHash))
                    return BadRequest("Current password is incorrect.");

                // Validate new password
                if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 6)
                    return BadRequest("New password must be at least 6 characters long.");

                if (dto.NewPassword != dto.ConfirmPassword)
                    return BadRequest("New password and confirmation do not match.");

                // Update password
                tutor.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
                await _context.SaveChangesAsync();

                return Ok("Password changed successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error changing password: {ex.Message}");
            }
        }

    }
}
