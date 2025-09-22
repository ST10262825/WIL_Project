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
                IsVerified = true,

                // ✅ ADD RATING PROPERTIES FOR BROWSE VIEW TOO
                AverageRating = t.AverageRating,
                TotalReviews = t.TotalReviews
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


    }
}
