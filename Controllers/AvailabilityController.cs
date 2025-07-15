using Microsoft.AspNetCore.Mvc;
using global::TutorConnectAPI.Data;
using global::TutorConnectAPI.DTOs;
using global::TutorConnectAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace TutorConnectAPI.Controllers
{
   
    

    namespace TutorConnectAPI.Controllers
    {
        [ApiController]
        [Route("api/availability")]
        public class AvailabilityController : ControllerBase
        {
            private readonly ApplicationDbContext _context;

            public AvailabilityController(ApplicationDbContext context)
            {
                _context = context;
            }

            [Authorize(Roles = "Tutor")]
            [HttpPost]
            public async Task<IActionResult> AddAvailability(AvailabilityDTO dto)
            {
                if (dto.EndTime <= dto.StartTime)
                    return BadRequest("End time must be after start time.");

                var availability = new Availability
                {
                    TutorId = dto.TutorId,
                    StartTime = dto.StartTime,
                    EndTime = dto.EndTime
                };

                _context.Availabilities.Add(availability);
                await _context.SaveChangesAsync();

                return Ok("Availability slot added.");
            }

            [HttpGet("tutor/{tutorId}")]
            public async Task<IActionResult> GetAvailability(int tutorId)
            {
                var slots = await _context.Availabilities
                    .Where(a => a.TutorId == tutorId)
                    .ToListAsync();

                return Ok(slots);
            }
        }
    }

}
