using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TutorConnectAPI.Data;
using TutorConnectAPI.DTOs;
using TutorConnectAPI.Models;

namespace TutorConnectAPI.Controllers
{
    [ApiController]
    [Route("api/admin")]
    //[Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("create-tutor")]
        public async Task<IActionResult> CreateTutor(CreateTutorDTO dto)
        {
            string normalizedEmail = dto.Email.Trim().ToLower();

            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

            if (existingUser != null)
                return BadRequest("A user with this email already exists.");

            var user = new User
            {
                Email = normalizedEmail,
                Role = "Tutor",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                IsEmailVerified = true
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var tutor = new Tutor
            {
                UserId = user.UserId,
                Name = dto.Name,
                Surname = dto.Surname,
                Phone = dto.Phone,
                Bio = dto.Bio,
            };

            _context.Tutors.Add(tutor);
            await _context.SaveChangesAsync();

            foreach (var moduleId in dto.ModuleIds)
            {
                _context.TutorModules.Add(new TutorModule
                {
                    TutorId = tutor.TutorId,
                    ModuleId = moduleId
                });
            }

            await _context.SaveChangesAsync();

            return Ok("Tutor created successfully.");
        }

        [HttpGet("tutors")]
        public async Task<IActionResult> GetTutors()
        {
            var tutors = await _context.Tutors
                .Include(t => t.User)
                .Include(t => t.TutorModules)
                    .ThenInclude(tm => tm.Module)
                .ToListAsync();

            var tutorDtos = tutors.Select(t => new TutorDTO
            {
                TutorId = t.TutorId,
                Name = t.Name,
                Surname = t.Surname,
                Phone = t.Phone,
                Bio = t.Bio,
                IsBlocked = t.IsBlocked,

                Modules = t.TutorModules.Select(tm => new ModuleDTO
                {
                    Id = tm.Module.ModuleId,
                    Code = tm.Module.Code,
                    Name = tm.Module.Name
                }).ToList(),

                Expertise = t.Expertise,           // Add these to mapping
                Education = t.Education,
                ProfileImageUrl = t.ProfileImageUrl
            }).ToList();



            return Ok(tutorDtos);
        }


        

        [HttpDelete("delete-tutor/{id}")]
        public async Task<IActionResult> DeleteTutor(int id)
        {
            var tutor = await _context.Tutors
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.TutorId == id);

            if (tutor == null)
                return NotFound("Tutor not found.");

            _context.Users.Remove(tutor.User); // This will also delete the tutor if cascade is set
            await _context.SaveChangesAsync();

            return Ok("Tutor deleted successfully.");
        }

        [HttpPut("block-tutor/{id}")]
        public async Task<IActionResult> BlockTutor(int id)
        {
            var tutor = await _context.Tutors.FirstOrDefaultAsync(t => t.TutorId == id);
            if (tutor == null)
                return NotFound("Tutor not found.");

            tutor.IsBlocked = true;
            await _context.SaveChangesAsync();

            return Ok("Tutor blocked.");
        }

        [HttpPut("unblock-tutor/{id}")]
        public async Task<IActionResult> UnblockTutor(int id)
        {
            var tutor = await _context.Tutors.FirstOrDefaultAsync(t => t.TutorId == id);
            if (tutor == null)
                return NotFound("Tutor not found.");

            tutor.IsBlocked = false;
            await _context.SaveChangesAsync();

            return Ok("Tutor unblocked.");
        }

        [HttpGet("tutors/{id}")]
        public async Task<IActionResult> GetTutorById(int id)
        {
            var tutor = await _context.Tutors
                .Include(t => t.User)
                .Include(t => t.TutorModules)
                    .ThenInclude(tm => tm.Module)
                .FirstOrDefaultAsync(t => t.TutorId == id);

            if (tutor == null)
                return NotFound("Tutor not found.");

            var dto = new TutorDTO
            {
                TutorId = tutor.TutorId,
                Name = tutor.Name,
                Surname = tutor.Surname,
                Phone = tutor.Phone,
                Bio = tutor.Bio,
                IsBlocked = tutor.IsBlocked,
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
