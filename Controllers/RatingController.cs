using Microsoft.AspNetCore.Mvc;
using global::TutorConnectAPI.Data;
using global::TutorConnectAPI.DTOs;
using global::TutorConnectAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;


namespace TutorConnectAPI.Controllers
{
    

    namespace TutorConnectAPI.Controllers
    {
        [ApiController]
        [Route("api/rating")]
        public class RatingController : ControllerBase
        {
            private readonly ApplicationDbContext _context;

            public RatingController(ApplicationDbContext context)
            {
                _context = context;
            }

            [Authorize(Roles = "Student")]
            [HttpPost]
            public async Task<IActionResult> RateTutor(RatingDTO dto)
            {
                if (dto.Stars < 1 || dto.Stars > 5)
                    return BadRequest("Rating must be between 1 and 5.");

                var rating = new Rating
                {
                    TutorId = dto.TutorId,
                    StudentId = dto.StudentId,
                    Stars = dto.Stars,
                    Comment = dto.Comment
                };

                _context.Ratings.Add(rating);
                await _context.SaveChangesAsync();

                return Ok("Tutor rated successfully.");
            }

            [HttpGet("tutor/{tutorId}")]
            public async Task<IActionResult> GetTutorRatings(int tutorId)
            {
                var ratings = await _context.Ratings
                    .Where(r => r.TutorId == tutorId)
                    .ToListAsync();

                return Ok(ratings);
            }
        }
    }

}
