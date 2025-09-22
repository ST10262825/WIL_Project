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
            try
            {
                Console.WriteLine($"Creating tutor with email: {dto.Email}");
                Console.WriteLine($"Module IDs: {string.Join(", ", dto.ModuleIds)}");

                // Validate input
                if (dto.ModuleIds == null || !dto.ModuleIds.Any())
                {
                    return BadRequest("At least one module must be selected.");
                }

                string normalizedEmail = dto.Email.Trim().ToLower();

                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

                if (existingUser != null)
                    return BadRequest("A user with this email already exists.");

                // Verify all modules exist
                var validModuleIds = dto.ModuleIds.Where(id => id > 0).Distinct().ToList();
                var existingModules = await _context.Modules
                    .Where(m => validModuleIds.Contains(m.ModuleId))
                    .ToListAsync();

                if (existingModules.Count != validModuleIds.Count)
                {
                    var existingModuleIds = existingModules.Select(m => m.ModuleId).ToList();
                    var missingModules = validModuleIds.Except(existingModuleIds).ToList();
                    return BadRequest($"The following modules do not exist: {string.Join(", ", missingModules)}");
                }

                // Create user
                var user = new User
                {
                    Email = normalizedEmail,
                    Role = "Tutor",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                    IsEmailVerified = true
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Create tutor
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

                // Add tutor modules using the actual entity objects
                foreach (var module in existingModules)
                {
                    var tutorModule = new TutorModule
                    {
                        TutorId = tutor.TutorId,
                        ModuleId = module.ModuleId
                    };
                    _context.TutorModules.Add(tutorModule);
                }

                await _context.SaveChangesAsync();

                return Ok("Tutor created successfully.");
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"Database error: {ex.InnerException?.Message ?? ex.Message}");
                return StatusCode(500, $"Database error: {ex.InnerException?.Message ?? ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        [HttpGet("tutors")]
        public async Task<IActionResult> GetTutors()
        {
            try
            {
                var tutors = await _context.Tutors
                    .Include(t => t.User)
                    .Include(t => t.TutorModules)
                        .ThenInclude(tm => tm.Module)
                    .ToListAsync();

                var tutorIds = tutors.Select(t => t.TutorId).ToList();

                // Get booking counts
                var bookingCounts = await _context.Bookings
                    .Where(b => tutorIds.Contains(b.TutorId))
                    .GroupBy(b => b.TutorId)
                    .Select(g => new { TutorId = g.Key, TotalBookings = g.Count() })
                    .ToListAsync();

                // Get rating data
                var ratingData = await _context.Reviews
                    .Where(r => tutorIds.Contains(r.TutorId))
                    .GroupBy(r => r.TutorId)
                    .Select(g => new
                    {
                        TutorId = g.Key,
                        AverageRating = g.Average(r => (double)r.Rating),
                        TotalReviews = g.Count()
                    })
                    .ToListAsync();

                var tutorDtos = tutors.Select(t => new TutorDTO
                {
                    TutorId = t.TutorId,
                    UserId = t.UserId,
                    Name = t.Name,
                    Surname = t.Surname,
                    Phone = t.Phone,
                    Email = t.User.Email,
                    Bio = t.Bio,
                    IsBlocked = t.IsBlocked,
                    ProfileImageUrl = t.ProfileImageUrl,
                    AboutMe = t.AboutMe,
                    Expertise = t.Expertise,
                    Education = t.Education,
                    Modules = t.TutorModules.Select(tm => new ModuleDTO
                    {
                        ModuleId = tm.ModuleId,
                        Code = tm.Module.Code,
                        Name = tm.Module.Name
                    }).ToList(),

                    // Add the computed properties
                    TotalBookings = bookingCounts.FirstOrDefault(b => b.TutorId == t.TutorId)?.TotalBookings ?? 0,
                    AverageRating = ratingData.FirstOrDefault(r => r.TutorId == t.TutorId)?.AverageRating ?? 0,
                    TotalReviews = ratingData.FirstOrDefault(r => r.TutorId == t.TutorId)?.TotalReviews ?? 0
                }).ToList();

                return Ok(tutorDtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        [HttpPut("update-tutor/{id}")]
        public async Task<IActionResult> UpdateTutor(int id, [FromBody] AdminUpdateTutorDTO dto)
        {
            if (id != dto.Id)
                return BadRequest("ID mismatch");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var tutor = await _context.Tutors
                    .Include(t => t.User)
                    .Include(t => t.TutorModules)
                    .FirstOrDefaultAsync(t => t.TutorId == id);

                if (tutor == null)
                    return NotFound("Tutor not found");

                // Update basic tutor information
                tutor.Name = dto.Name;
                tutor.Surname = dto.Surname;
                tutor.Phone = dto.Phone;
                tutor.Bio = dto.Bio;
                tutor.AboutMe = dto.AboutMe;
                tutor.Expertise = dto.Expertise;
                tutor.Education = dto.Education;
                tutor.IsBlocked = dto.IsBlocked;

                // Update user email if provided
                if (!string.IsNullOrEmpty(dto.Email))
                {
                    var normalizedEmail = dto.Email.Trim().ToLower();

                    // Check if email is already taken by another user
                    var emailExists = await _context.Users
                        .AnyAsync(u => u.Email.ToLower() == normalizedEmail && u.UserId != tutor.UserId);

                    if (emailExists)
                        return BadRequest("Email is already taken by another user");

                    tutor.User.Email = normalizedEmail;
                }

                // Update modules
                var currentModuleIds = tutor.TutorModules.Select(tm => tm.ModuleId).ToList();
                var modulesToRemove = currentModuleIds.Except(dto.ModuleIds).ToList();
                var modulesToAdd = dto.ModuleIds.Except(currentModuleIds).ToList();

                // Remove modules
                if (modulesToRemove.Any())
                {
                    var modulesToRemoveEntities = tutor.TutorModules
                        .Where(tm => modulesToRemove.Contains(tm.ModuleId))
                        .ToList();

                    _context.TutorModules.RemoveRange(modulesToRemoveEntities);
                }

                // Add new modules
                foreach (var moduleId in modulesToAdd)
                {
                    // Verify module exists
                    var moduleExists = await _context.Modules.AnyAsync(m => m.ModuleId == moduleId);
                    if (!moduleExists)
                        return BadRequest($"Module with ID {moduleId} does not exist");

                    _context.TutorModules.Add(new TutorModule
                    {
                        TutorId = tutor.TutorId,
                        ModuleId = moduleId
                    });
                }

                await _context.SaveChangesAsync();

                return Ok(new { message = "Tutor updated successfully" });
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, $"Database error: {ex.InnerException?.Message ?? ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
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
                Email = tutor.User.Email,
                IsBlocked = tutor.IsBlocked,
                AboutMe = tutor.AboutMe,
                Expertise = tutor.Expertise,
                Education = tutor.Education,
                ProfileImageUrl = tutor.ProfileImageUrl,
                Modules = tutor.TutorModules.Select(tm => new ModuleDTO
                {
                    ModuleId = tm.ModuleId,
                    Code = tm.Module.Code,
                    Name = tm.Module.Name
                }).ToList()
            };

            return Ok(dto);
        }





        [HttpDelete("delete-tutor/{id}")]
        public async Task<IActionResult> DeleteTutor(int id)
        {
            try
            {
                Console.WriteLine($"Attempting to delete tutor with ID: {id}");

                var tutor = await _context.Tutors
                    .Include(t => t.User)
                    .Include(t => t.TutorModules)
                    .Include(t => t.Reviews)
                    .Include(t => t.Bookings)
                    .FirstOrDefaultAsync(t => t.TutorId == id);

                if (tutor == null)
                {
                    Console.WriteLine($"Tutor with ID {id} not found");
                    return NotFound(new { message = "Tutor not found." });
                }

                Console.WriteLine($"Found tutor: {tutor.Name} {tutor.Surname}");

                // Check for any bookings
                var hasAnyBookings = await _context.Bookings.AnyAsync(b => b.TutorId == id);
                if (hasAnyBookings)
                {
                    Console.WriteLine($"Tutor has bookings, checking statuses...");

                    var pendingBookings = await _context.Bookings
                        .AnyAsync(b => b.TutorId == id && b.Status == "Pending");

                    var activeBookings = await _context.Bookings
                        .AnyAsync(b => b.TutorId == id && (b.Status == "Confirmed" || b.Status == "Scheduled"));

                    if (pendingBookings)
                    {
                        Console.WriteLine("Tutor has pending bookings");
                        return BadRequest(new
                        {
                            message = "Cannot delete tutor with pending bookings. Please resolve or cancel pending bookings first.",
                            hasPendingBookings = true
                        });
                    }

                    if (activeBookings)
                    {
                        Console.WriteLine("Tutor has active bookings");
                        return BadRequest(new
                        {
                            message = "Cannot delete tutor with active upcoming sessions. Please resolve all bookings first.",
                            hasActiveBookings = true
                        });
                    }
                }

                // Remove related entities first to avoid constraint issues
                Console.WriteLine("Removing related entities...");

                // Remove chat messages where user is sender or receiver
                var chatMessages = await _context.ChatMessages
                    .Where(cm => cm.SenderId == tutor.UserId || cm.ReceiverId == tutor.UserId)
                    .ToListAsync();

                if (chatMessages.Any())
                {
                    _context.ChatMessages.RemoveRange(chatMessages);
                    Console.WriteLine($"Removed {chatMessages.Count} chat messages");
                }

                // Remove tutor modules
                var tutorModules = await _context.TutorModules
                    .Where(tm => tm.TutorId == id)
                    .ToListAsync();
                _context.TutorModules.RemoveRange(tutorModules);
                Console.WriteLine($"Removed {tutorModules.Count} tutor modules");

                // Remove reviews
                var reviews = await _context.Reviews
                    .Where(r => r.TutorId == id)
                    .ToListAsync();
                if (reviews.Any())
                {
                    _context.Reviews.RemoveRange(reviews);
                    Console.WriteLine($"Removed {reviews.Count} reviews");
                }

                // Remove bookings
                var bookings = await _context.Bookings
                    .Where(b => b.TutorId == id)
                    .ToListAsync();
                if (bookings.Any())
                {
                    _context.Bookings.RemoveRange(bookings);
                    Console.WriteLine($"Removed {bookings.Count} bookings");
                }

                // Remove tutor
                _context.Tutors.Remove(tutor);
                Console.WriteLine("Removed tutor");

                // Remove user account (this will be handled by cascade delete if set up properly)
                _context.Users.Remove(tutor.User);
                Console.WriteLine("Removed user account");

                await _context.SaveChangesAsync();
                Console.WriteLine("Changes saved successfully");

                return Ok(new { message = "Tutor deleted successfully." });
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"Database error: {ex.InnerException?.Message ?? ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                // Check if it's a specific constraint violation
                if (ex.InnerException != null && ex.InnerException.Message.Contains("FK_ChatMessages_Users"))
                {
                    return BadRequest(new
                    {
                        message = "Cannot delete tutor due to existing chat messages. Please try again or contact system administrator.",
                        hasChatMessages = true
                    });
                }

                return StatusCode(500, new { message = $"Database error: {ex.InnerException?.Message ?? ex.Message}" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                return StatusCode(500, new { message = $"An unexpected error occurred: {ex.Message}" });
            }
        }



        // ------------------ Students ------------------
        [HttpGet("students")]
        public async Task<IActionResult> GetStudents()
        {
            var students = await _context.Students.ToListAsync();
            return Ok(students);
        }

        [HttpDelete("delete-student/{id}")]
        public async Task<IActionResult> DeleteStudent(int id)
        {
            try
            {
                Console.WriteLine($"API: Delete student request for ID: {id}");

                var student = await _context.Students
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.StudentId == id);

                if (student == null)
                {
                    Console.WriteLine($"Student with ID {id} not found");
                    return NotFound(new { message = "Student not found." });
                }

                // Check if student has any bookings
                var hasBookings = await _context.Bookings
                    .AnyAsync(b => b.StudentId == id);

                if (hasBookings)
                {
                    Console.WriteLine($"Student {id} has bookings, cannot delete");
                    return BadRequest(new
                    {
                        message = "Cannot delete student with existing bookings. Please delete or reassign bookings first.",
                        hasBookings = true
                    });
                }

                // Remove student record
                _context.Students.Remove(student);

                // Remove user account
                _context.Users.Remove(student.User);

                await _context.SaveChangesAsync();

                Console.WriteLine($"Student {id} deleted successfully");
                return Ok(new { message = "Student deleted successfully." });
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"Database error deleting student: {ex.InnerException?.Message ?? ex.Message}");
                return StatusCode(500, new { message = "Database error occurred while deleting student." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting student: {ex.Message}");
                return StatusCode(500, new { message = $"An error occurred: {ex.Message}" });
            }
        }

        [HttpPut("block-student/{id}")]
        public async Task<IActionResult> BlockStudent(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null) return NotFound("Student not found.");
            student.IsBlocked = true;
            await _context.SaveChangesAsync();
            return Ok("Student blocked.");
        }

        [HttpPut("unblock-student/{id}")]
        public async Task<IActionResult> UnblockStudent(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null) return NotFound("Student not found.");
            student.IsBlocked = false;
            await _context.SaveChangesAsync();
            return Ok("Student unblocked.");
        }

        // ------------------ Bookings ------------------
        [HttpGet("bookings")]
        public async Task<IActionResult> GetBookings()
        {
            try
            {
                var bookings = await _context.Bookings
                    .Include(b => b.Student)
                    .Include(b => b.Tutor)
                    .Include(b => b.Module) // Make sure you have this relationship
                    .ToListAsync();

                var bookingDtos = bookings.Select(b => new BookingDTO
                {
                    BookingId = b.BookingId,
                    StudentId = b.StudentId,
                    TutorId = b.TutorId,
                    StudentName = b.Student != null ? $"{b.Student.Name}" : "N/A",
                    TutorName = b.Tutor != null ? $"{b.Tutor.Name} {b.Tutor.Surname}" : "N/A",
                    ModuleName = b.Module != null ? b.Module.Name : "N/A",
                    StartTime = b.StartTime,
                    EndTime = b.EndTime,
                    Status = b.Status,
                    Notes = b.Notes

                }).ToList();

                return Ok(bookingDtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpDelete("delete-booking/{id}")]
        public async Task<IActionResult> DeleteBooking(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Review) // Include related review
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null)
                return NotFound("Booking not found.");

            if (booking.Status == "Pending")
                return BadRequest("Cannot delete a pending booking.");

            // Delete associated review first
            if (booking.Review != null)
                _context.Reviews.Remove(booking.Review);

            _context.Bookings.Remove(booking);
            await _context.SaveChangesAsync();

            return Ok("Booking deleted successfully along with its review.");
        }




        // ------------------ Modules ------------------
        [HttpGet("modules")]
        public async Task<IActionResult> GetModules()
        {
            try
            {
                var modules = await _context.Modules.ToListAsync();

                // Get tutor counts for each module
                var tutorCounts = await _context.TutorModules
                    .GroupBy(tm => tm.ModuleId)
                    .Select(g => new { ModuleId = g.Key, Count = g.Count() })
                    .ToListAsync();

                // Get booking counts for each module (assuming bookings have module association)
                var bookingCounts = await _context.Bookings
                    .Include(b => b.Module) // If you have this relationship
                    .Where(b => b.ModuleId != null)
                    .GroupBy(b => b.ModuleId)
                    .Select(g => new { ModuleId = g.Key, Count = g.Count() })
                    .ToListAsync();

                var moduleDtos = modules.Select(m => new ModuleDTO
                {
                    ModuleId = m.ModuleId,
                    Code = m.Code,
                    Name = m.Name,
                    TutorCount = tutorCounts.FirstOrDefault(tc => tc.ModuleId == m.ModuleId)?.Count ?? 0,
                    BookingCount = bookingCounts.FirstOrDefault(bc => bc.ModuleId == m.ModuleId)?.Count ?? 0
                }).ToList();

                return Ok(moduleDtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }




        [HttpPost("create-module")]
        public async Task<IActionResult> CreateModule([FromBody] CreateModuleRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                // Check if module code already exists
                var existingModule = await _context.Modules
                    .FirstOrDefaultAsync(m => m.Code.ToLower() == request.Code.Trim().ToLower());

                if (existingModule != null)
                    return BadRequest("Module code already exists.");

                var module = new Module
                {
                    Code = request.Code.Trim().ToUpper(),
                    Name = request.Name.Trim()
                };

                _context.Modules.Add(module);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Module created successfully.", moduleId = module.ModuleId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        [HttpPut("update-module/{id}")]
        public async Task<IActionResult> UpdateModule(int id, [FromBody] UpdateModuleRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var module = await _context.Modules.FindAsync(id);
                if (module == null)
                    return NotFound("Module not found.");

                // Check if new code conflicts with existing modules (excluding current)
                var codeExists = await _context.Modules
                    .AnyAsync(m => m.Code.ToLower() == request.Code.Trim().ToLower() && m.ModuleId != id);

                if (codeExists)
                    return BadRequest("Module code already exists.");

                module.Code = request.Code.Trim().ToUpper();
                module.Name = request.Name.Trim();

                _context.Modules.Update(module);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Module updated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }



        [HttpDelete("delete-module/{id}")]
        public async Task<IActionResult> DeleteModule(int id)
        {
            try
            {
                var module = await _context.Modules.FindAsync(id);
                if (module == null)
                    return NotFound("Module not found.");

                // Check if module is assigned to any tutors
                var hasTutors = await _context.TutorModules.AnyAsync(tm => tm.ModuleId == id);
                if (hasTutors)
                {
                    return BadRequest(new
                    {
                        message = "Cannot delete module. It is currently assigned to one or more tutors.",
                        hasTutors = true
                    });
                }

                // Check if module has any bookings (if your bookings have module association)
                var hasBookings = await _context.Bookings.AnyAsync(b => b.ModuleId == id);
                if (hasBookings)
                {
                    return BadRequest(new
                    {
                        message = "Cannot delete module. It has existing bookings.",
                        hasBookings = true
                    });
                }

                _context.Modules.Remove(module);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Module deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }

}