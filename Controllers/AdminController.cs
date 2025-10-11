using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TutorConnectAPI.Data;
using TutorConnectAPI.DTOs;
using TutorConnectAPI.Models;
using System.Text;
using ClosedXML.Excel;
using iTextSharp.text;
using iTextSharp.text.pdf;

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

                // Include ALL related entities to check for dependencies
                var student = await _context.Students
                    .Include(s => s.User)
                    .Include(s => s.Bookings)
                        .ThenInclude(b => b.Review) // Include booking reviews
                    .Include(s => s.Reviews) // Reviews written by student
                    .FirstOrDefaultAsync(s => s.StudentId == id);

                if (student == null)
                {
                    Console.WriteLine($"Student with ID {id} not found");
                    return NotFound(new { message = "Student not found." });
                }

                Console.WriteLine($"Found student: {student.Name}");

                // 1. Check for any ACTIVE bookings (more comprehensive check)
                var hasActiveBookings = await _context.Bookings
                    .AnyAsync(b => b.StudentId == id &&
                                  (b.Status == "Pending" || b.Status == "Confirmed" || b.Status == "Scheduled"));

                if (hasActiveBookings)
                {
                    Console.WriteLine($"Student {id} has active bookings, cannot delete");
                    return BadRequest(new
                    {
                        message = "Cannot delete student with active or pending bookings. Please resolve all bookings first.",
                        hasActiveBookings = true
                    });
                }

                // 2. Check for COMPLETED bookings with reviews
                var hasCompletedBookingsWithReviews = await _context.Bookings
                    .Include(b => b.Review)
                    .AnyAsync(b => b.StudentId == id && b.Status == "Completed" && b.Review != null);

                if (hasCompletedBookingsWithReviews)
                {
                    Console.WriteLine($"Student {id} has completed bookings with reviews");
                    return BadRequest(new
                    {
                        message = "Cannot delete student with historical reviews. Please contact administrator.",
                        hasReviews = true
                    });
                }

                // 3. Remove related entities in correct order to avoid constraint violations

                // Remove reviews written BY this student
                var studentReviews = await _context.Reviews
                    .Where(r => r.StudentId == id)
                    .ToListAsync();
                if (studentReviews.Any())
                {
                    _context.Reviews.RemoveRange(studentReviews);
                    Console.WriteLine($"Removed {studentReviews.Count} reviews written by student");
                }

                // Remove chat messages where student is sender or receiver
                var chatMessages = await _context.ChatMessages
                    .Where(cm => cm.SenderId == student.UserId || cm.ReceiverId == student.UserId)
                    .ToListAsync();
                if (chatMessages.Any())
                {
                    _context.ChatMessages.RemoveRange(chatMessages);
                    Console.WriteLine($"Removed {chatMessages.Count} chat messages");
                }

                // Remove completed bookings (without reviews)
                var completedBookings = await _context.Bookings
                    .Where(b => b.StudentId == id && b.Status == "Completed")
                    .ToListAsync();
                if (completedBookings.Any())
                {
                    _context.Bookings.RemoveRange(completedBookings);
                    Console.WriteLine($"Removed {completedBookings.Count} completed bookings");
                }

                // Remove any other bookings (cancelled, etc.)
                var otherBookings = await _context.Bookings
                    .Where(b => b.StudentId == id)
                    .ToListAsync();
                if (otherBookings.Any())
                {
                    _context.Bookings.RemoveRange(otherBookings);
                    Console.WriteLine($"Removed {otherBookings.Count} other bookings");
                }

                // Remove student record
                _context.Students.Remove(student);
                Console.WriteLine("Removed student record");

                // Remove user account (this should be last)
                _context.Users.Remove(student.User);
                Console.WriteLine("Removed user account");

                await _context.SaveChangesAsync();
                Console.WriteLine($"Student {id} deleted successfully");

                return Ok(new { message = "Student deleted successfully." });
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"Database error deleting student: {ex.InnerException?.Message ?? ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                // Provide specific error messages based on constraint
                if (ex.InnerException?.Message.Contains("FK_Reviews_Students") == true)
                {
                    return BadRequest(new
                    {
                        message = "Cannot delete student due to existing reviews. Please contact administrator.",
                        hasReviews = true
                    });
                }

                return StatusCode(500, new { message = $"Database error: {ex.InnerException?.Message ?? ex.Message}" });
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

        [HttpPost("generate-report")]
        public async Task<IActionResult> GenerateReport([FromBody] ReportFilterDTO filters)
        {
            try
            {
                // Add logging to see what's being received
                Console.WriteLine("=== REPORT GENERATION DEBUG ===");
                Console.WriteLine($"ReportType: {filters?.ReportType}");
                Console.WriteLine($"DateRange: {filters?.DateRange}");
                Console.WriteLine($"ExportFormat: {filters?.ExportFormat}");
                Console.WriteLine($"StartDate: {filters?.StartDate}");
                Console.WriteLine($"EndDate: {filters?.EndDate}");
                Console.WriteLine($"UserType: {filters?.UserType}");
                Console.WriteLine($"UserId: {filters?.UserId}");
                Console.WriteLine($"Statuses: {(filters?.Statuses != null ? string.Join(", ", filters.Statuses.Select(s => s.ToString())) : "null")}");
                Console.WriteLine($"ModuleIds: {(filters?.ModuleIds != null ? string.Join(", ", filters.ModuleIds.Select(m => m.ToString())) : "null")}");
                Console.WriteLine("=== END DEBUG ===");

                // Validate input
                if (filters == null)
                {
                    return BadRequest("Please provide report filters.");
                }

                if (string.IsNullOrEmpty(filters.ReportType))
                {
                    return BadRequest("Report type is required.");
                }

                // Validate report type
                var validReportTypes = new[] { "Student", "Tutor", "Booking", "ModuleDemand", "Everything"};
                if (!validReportTypes.Any(rt => rt.Equals(filters.ReportType, StringComparison.OrdinalIgnoreCase)))
                {
                    return BadRequest($"Invalid report type. Please select one of: {string.Join(", ", validReportTypes)}");
                }


                // Validate date range for custom dates
                if (filters.DateRange?.ToLower() == "custom")
                {
                    if (!filters.StartDate.HasValue || !filters.EndDate.HasValue)
                    {
                        return BadRequest("Both start date and end date are required for custom date range.");
                    }

                    if (filters.StartDate.Value > filters.EndDate.Value)
                    {
                        return BadRequest("Start date cannot be after end date.");
                    }

                    if (filters.StartDate.Value > DateTime.UtcNow)
                    {
                        return BadRequest("Start date cannot be in the future.");
                    }
                }

                // Validate UserId if UserType is provided
                if (!string.IsNullOrEmpty(filters.UserType) && !filters.UserId.HasValue)
                {
                    return BadRequest("Please select a user when filtering by user type.");
                }

                // Validate ModuleIds if provided
                if (filters.ModuleIds != null && filters.ModuleIds.Any())
                {
                    var validModuleIds = await _context.Modules
                        .Where(m => filters.ModuleIds.Contains(m.ModuleId))
                        .Select(m => m.ModuleId)
                        .ToListAsync();

                    var invalidModuleIds = filters.ModuleIds.Except(validModuleIds).ToList();
                    if (invalidModuleIds.Any())
                    {
                        return BadRequest($"Invalid module IDs: {string.Join(", ", invalidModuleIds)}");
                    }
                }

                // Validate UserId exists if provided
                if (filters.UserId.HasValue)
                {
                    if (filters.UserType == "Student")
                    {
                        var studentExists = await _context.Students.AnyAsync(s => s.StudentId == filters.UserId.Value);
                        if (!studentExists)
                        {
                            return BadRequest("The selected student does not exist.");
                        }
                    }
                    else if (filters.UserType == "Tutor")
                    {
                        var tutorExists = await _context.Tutors.AnyAsync(t => t.TutorId == filters.UserId.Value);
                        if (!tutorExists)
                        {
                            return BadRequest("The selected tutor does not exist.");
                        }
                    }
                }

                // Validate statuses based on report type
                if (filters.Statuses != null && filters.Statuses.Any())
                {
                    var validStatuses = GetValidStatusesForReportType(filters.ReportType);
                    var invalidStatuses = filters.Statuses.Except(validStatuses).ToList();

                    if (invalidStatuses.Any())
                    {
                        return BadRequest($"Invalid statuses for {filters.ReportType} report: {string.Join(", ", invalidStatuses)}. Valid statuses are: {string.Join(", ", validStatuses)}");
                    }
                }

                var reportResult = await GenerateReportData(filters);

                if (!string.IsNullOrEmpty(filters.ExportFormat))
                {
                    return filters.ExportFormat.ToLower() switch
                    {
                        "excel" => GenerateExcelReport(reportResult),
                        "pdf" => GeneratePdfReport(reportResult),
                        "csv" => GenerateCsvReport(reportResult),
                        _ => BadRequest($"Unsupported export format: {filters.ExportFormat}. Supported formats: Excel, PDF, CSV")
                    };
                }

                return Ok(reportResult);
            }
            catch (ArgumentException ex)
            {
                // This handles cases where invalid report types are passed to GenerateReportData
                Console.WriteLine($"Argument error generating report: {ex.Message}");
                return BadRequest(ex.Message);
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"Database error generating report: {ex.Message}");
                return StatusCode(500, "A database error occurred while generating the report. Please try again.");
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"Data processing error generating report: {ex.Message}");
                return BadRequest($"Data processing error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error generating report: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, "An unexpected error occurred while generating the report. Please try again later.");
            }
        }

        // Helper method to get valid statuses for each report type
        private List<string> GetValidStatusesForReportType(string reportType)
        {
            return reportType.ToLower() switch
            {
                "student" or "tutor" => new List<string> { "Active", "Inactive" },
                "booking" => new List<string> { "Pending", "Confirmed", "Completed", "Cancelled" },
                "moduledemand" => new List<string>(),
                _ => new List<string>()
            };
        }


        [HttpGet("report-options")]
        public async Task<IActionResult> GetReportOptions()
        {
            var options = new
            {
                Modules = await _context.Modules.Select(m => new { m.ModuleId, m.Name, m.Code }).ToListAsync(),
                Tutors = await _context.Tutors.Where(t => !t.IsBlocked)
                            .Select(t => new { t.TutorId, Name = $"{t.Name} {t.Surname}" }).ToListAsync(),
                Students = await _context.Students.Where(s => !s.IsBlocked)
                            .Select(s => new { s.StudentId, Name = $"{s.Name}" }).ToListAsync(),
                Statuses = new[] { "Pending", "Confirmed", "Completed", "Cancelled", "Active", "Inactive" },
                ReportTypes = new[] { "Student", "Tutor", "Booking", "ModuleDemand", "Everything" } // UPDATED
            };

            return Ok(options);
        }

        private async Task<ReportResultDTO> GenerateReportData(ReportFilterDTO filters)
        {
            // Apply date range filter
            var dateRange = GetDateRange(filters);
            filters.StartDate = dateRange.StartDate;
            filters.EndDate = dateRange.EndDate;

            if (filters.ReportType?.ToLower() == "everything")
            {
                filters.StartDate ??= new DateTime(2000, 1, 1);
                filters.EndDate ??= DateTime.UtcNow;
                filters.Statuses ??= new List<string>();
                filters.ModuleIds ??= new List<int>();
            }


            return filters.ReportType?.ToLower() switch
            {
                "student" => await GenerateStudentReport(filters),
                "tutor" => await GenerateTutorReport(filters),
                "booking" => await GenerateBookingReport(filters),
                "moduledemand" => await GenerateModuleDemandReport(filters),
                "everything" => await GenerateEverythingReport(filters), // NEW
                _ => throw new ArgumentException("Invalid report type")
            };
        }

        // NEW: Comprehensive Everything Report
        private async Task<ReportResultDTO> GenerateEverythingReport(ReportFilterDTO filters)
        {
            // Generate each report **sequentially** to avoid DbContext concurrency issues
            var studentReport = await GenerateStudentReport(filters);
            var tutorReport = await GenerateTutorReport(filters);
            var bookingReport = await GenerateBookingReport(filters);
            var moduleDemandReport = await GenerateModuleDemandReport(filters);

            var everythingData = new EverythingReportDTO
            {
                StudentReport = studentReport.Data as StudentReportDTO,
                TutorReport = tutorReport.Data as TutorReportDTO,
                BookingReport = bookingReport.Data as BookingReportDTO,
                ModuleDemandReport = moduleDemandReport.Data as ModuleDemandReportDTO,

                // Overall summary
                OverallStats = new OverallStatsDTO
                {
                    TotalStudents = studentReport.Summary?.TotalRecords ?? 0,
                    TotalTutors = tutorReport.Summary?.TotalRecords ?? 0,
                    TotalBookings = bookingReport.Summary?.TotalRecords ?? 0,
                    TotalModules = moduleDemandReport.Summary?.TotalRecords ?? 0,
                    ActiveUsers = (studentReport.Summary?.ActiveUsers ?? 0) + (tutorReport.Summary?.ActiveUsers ?? 0),
                    
                    AverageRating = tutorReport.Summary?.AverageRating ?? 0
                }
            };

            return new ReportResultDTO
            {
                ReportTitle = "Comprehensive System Report",
                GeneratedAt = DateTime.UtcNow,
                Filters = filters,
                Data = everythingData,
                Summary = new SummaryDTO
                {
                    TotalRecords = everythingData.OverallStats.TotalStudents +
                                   everythingData.OverallStats.TotalTutors +
                                   everythingData.OverallStats.TotalBookings,
                    ActiveUsers = everythingData.OverallStats.ActiveUsers,
                    AverageRating = everythingData.OverallStats.AverageRating
                    
                }
            };
        }



        private async Task<ReportResultDTO> GenerateStudentReport(ReportFilterDTO filters)
        {
            var query = _context.Students
                .Include(s => s.User)
                .Include(s => s.Bookings)
                .AsQueryable();

            // Apply filters
            if (filters.UserId.HasValue)
                query = query.Where(s => s.StudentId == filters.UserId);

            //if (filters.StartDate.HasValue)
            //    query = query.Where(s => s.User.CreatedAt >= filters.StartDate);

            if (filters.Statuses.Contains("Active"))
                query = query.Where(s => !s.IsBlocked);
            if (filters.Statuses.Contains("Inactive"))
                query = query.Where(s => s.IsBlocked);

            var students = await query.ToListAsync();

            var reportData = new StudentReportDTO
            {
                Students = students.Select(s => new StudentReportItemDTO
                {
                    StudentId = s.StudentId,
                    Name = $"{s.Name}",
                    Email = s.User.Email,
                    //RegistrationDate = s.User.CreatedAt,
                    TotalBookings = s.Bookings.Count,
                    CompletedSessions = s.Bookings.Count(b => b.Status == "Completed"),
                    CancelledSessions = s.Bookings.Count(b => b.Status == "Cancelled"),
                    EngagementScore = s.Bookings.Any() ?
                        (decimal)s.Bookings.Count(b => b.Status == "Completed") / s.Bookings.Count * 100 : 0,
                    IsActive = !s.IsBlocked
                }).ToList(),

                TotalRegistrations = students.Count,
                TotalBookings = students.Sum(s => s.Bookings.Count),
                TotalCancellations = students.Sum(s => s.Bookings.Count(b => b.Status == "Cancelled")),
                AverageSessionsPerStudent = students.Any() ?
                    (decimal)students.Sum(s => s.Bookings.Count) / students.Count : 0
            };

            return new ReportResultDTO
            {
                ReportTitle = "Student Engagement Report",
                GeneratedAt = DateTime.UtcNow,
                Filters = filters,
                Data = reportData,
                Summary = new SummaryDTO
                {
                    TotalRecords = reportData.Students.Count,
                    ActiveUsers = reportData.Students.Count(s => s.IsActive)
                }
            };
        }

        private async Task<ReportResultDTO> GenerateTutorReport(ReportFilterDTO filters)
        {
            var query = _context.Tutors
                .Include(t => t.User)
                .Include(t => t.TutorModules).ThenInclude(tm => tm.Module)
                .Include(t => t.Bookings)
                .Include(t => t.Reviews)
                .AsQueryable();

            if (filters.UserId.HasValue)
                query = query.Where(t => t.TutorId == filters.UserId);

           // if (filters.StartDate.HasValue)
              //  query = query.Where(t => t.User.CreatedAt >= filters.StartDate);

            var tutors = await query.ToListAsync();

            var reportData = new TutorReportDTO
            {
                Tutors = tutors.Select(t => new TutorReportItemDTO
                {
                    TutorId = t.TutorId,
                    Name = $"{t.Name} {t.Surname}",
                    Email = t.User.Email,
                    Modules = t.TutorModules.Select(tm => tm.Module.Name).ToList(),
                    AverageRating = t.Reviews.Any() ? t.Reviews.Average(r => r.Rating) : 0,
                    TotalSessions = t.Bookings.Count,
                    CompletedSessions = t.Bookings.Count(b => b.Status == "Completed"),
                   // TotalEarnings = t.Bookings.Where(b => b.Status == "Completed").Sum(b => b.Amount ?? 0) * 0.8m, // 80% commission
                    IsActive = !t.IsBlocked,
                   // JoinDate = t.User.CreatedAt
                }).ToList(),

                ActiveTutors = tutors.Count(t => !t.IsBlocked),
                AverageRating = tutors.Any(t => t.Reviews.Any()) ?
                    (decimal)tutors.Where(t => t.Reviews.Any()).Average(t => t.Reviews.Average(r => r.Rating)) : 0,
                TotalCompletedSessions = tutors.Sum(t => t.Bookings.Count(b => b.Status == "Completed")),
               // TotalRevenueGenerated = tutors.Sum(t => t.Bookings.Where(b => b.Status == "Completed").Sum(b => b.Amount ?? 0))
            };

            return new ReportResultDTO
            {
                ReportTitle = "Tutor Performance Report",
                GeneratedAt = DateTime.UtcNow,
                Filters = filters,
                Data = reportData,
                Summary = new SummaryDTO
                {
                    TotalRecords = reportData.Tutors.Count,
                    AverageRating = reportData.AverageRating,
                    ActiveUsers = reportData.ActiveTutors
                }
            };
        }

        private async Task<ReportResultDTO> GenerateBookingReport(ReportFilterDTO filters)
        {
            var query = _context.Bookings
                .Include(b => b.Student)
                .Include(b => b.Tutor)
                .Include(b => b.Module)
                .AsQueryable();

            // Apply filters
            if (filters.StartDate.HasValue)
                query = query.Where(b => b.StartTime >= filters.StartDate);
            if (filters.EndDate.HasValue)
                query = query.Where(b => b.StartTime <= filters.EndDate);

            if (filters.Statuses.Any())
                query = query.Where(b => filters.Statuses.Contains(b.Status));

            if (filters.ModuleIds.Any())
                query = query.Where(b => filters.ModuleIds.Contains(b.ModuleId));

            var bookings = await query.ToListAsync();

            var reportData = new BookingReportDTO
            {
                Bookings = bookings.Select(b => new BookingReportItemDTO
                {
                    BookingId = b.BookingId,
                    StudentName = $"{b.Student.Name}",
                    TutorName = $"{b.Tutor.Name} {b.Tutor.Surname}",
                    Module = b.Module?.Name ?? "N/A",
                    SessionDate = b.StartTime,
                    Status = b.Status,
                    Duration = (int)(b.EndTime - b.StartTime).TotalHours
                }).ToList(),

                PendingCount = bookings.Count(b => b.Status == "Pending"),
                ConfirmedCount = bookings.Count(b => b.Status == "Confirmed"),
                CompletedCount = bookings.Count(b => b.Status == "Completed"),
                CancelledCount = bookings.Count(b => b.Status == "Cancelled"),
                //TotalRevenue = bookings.Where(b => b.Status == "Completed").Sum(b => b.Amount ?? 0)
            };

            return new ReportResultDTO
            {
                ReportTitle = "Booking Analysis Report",
                GeneratedAt = DateTime.UtcNow,
                Filters = filters,
                Data = reportData,
                Summary = new SummaryDTO
                {
                    TotalRecords = reportData.Bookings.Count,
                    TotalRevenue = reportData.TotalRevenue
                }
            };
        }

        private async Task<ReportResultDTO> GenerateModuleDemandReport(ReportFilterDTO filters)
        {
            var query = _context.Modules
                .Include(m => m.TutorModules)
                .Include(m => m.Bookings)
                .AsQueryable();

            if (filters.ModuleIds.Any())
                query = query.Where(m => filters.ModuleIds.Contains(m.ModuleId));

            var modules = await query.ToListAsync();

            var reportData = new ModuleDemandReportDTO
            {
                Modules = modules.Select(m => new ModuleDemandItemDTO
                {
                    ModuleId = m.ModuleId,
                    ModuleName = m.Name,
                    ModuleCode = m.Code,
                    TotalBookings = m.Bookings.Count(b =>
                        (!filters.StartDate.HasValue || b.StartTime >= filters.StartDate) &&
                        (!filters.EndDate.HasValue || b.StartTime <= filters.EndDate)),
                    UniqueTutors = m.TutorModules.Select(tm => tm.TutorId).Distinct().Count(),
                    UniqueStudents = m.Bookings.Select(b => b.StudentId).Distinct().Count(),
                    //TotalRevenue = m.Bookings.Where(b => b.Status == "Completed").Sum(b => b.Amount ?? 0),
                    GrowthRate = CalculateGrowthRate(m.ModuleId, filters.StartDate, filters.EndDate)
                }).OrderByDescending(m => m.TotalBookings).ToList(),

                TotalBookings = modules.Sum(m => m.Bookings.Count),
                MostPopularModule = modules.OrderByDescending(m => m.Bookings.Count).FirstOrDefault()?.Name,
                LeastPopularModule = modules.OrderBy(m => m.Bookings.Count).FirstOrDefault()?.Name
            };

            return new ReportResultDTO
            {
                ReportTitle = "Module Demand Analysis Report",
                GeneratedAt = DateTime.UtcNow,
                Filters = filters,
                Data = reportData,
                Summary = new SummaryDTO
                {
                    TotalRecords = reportData.Modules.Count
                }
            };
        }

        private double CalculateGrowthRate(int moduleId, DateTime? startDate, DateTime? endDate)
        {
            // Implementation for growth rate calculation
            return 0.0; // Placeholder
        }

        private (DateTime StartDate, DateTime EndDate) GetDateRange(ReportFilterDTO filters)
        {
            var now = DateTime.UtcNow;

            return filters.DateRange?.ToLower() switch
            {
                "last7days" => (now.AddDays(-7), now),
                "thismonth" => (new DateTime(now.Year, now.Month, 1), now),
                "lastmonth" => (new DateTime(now.Year, now.Month, 1).AddMonths(-1),
                               new DateTime(now.Year, now.Month, 1).AddDays(-1)),
                "custom" when filters.StartDate.HasValue && filters.EndDate.HasValue =>
                    (filters.StartDate.Value, filters.EndDate.Value),
                _ => (now.AddDays(-30), now) // Default to last 30 days
            };
        }

        // Export methods (simplified versions)
        private IActionResult GenerateExcelReport(ReportResultDTO report)
        {
            try
            {
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Report");

                // Add header
                worksheet.Cell(1, 1).Value = report.ReportTitle;
                worksheet.Cell(2, 1).Value = $"Generated on: {report.GeneratedAt:yyyy-MM-dd HH:mm}";
                worksheet.Cell(3, 1).Value = $"Total Records: {report.Summary?.TotalRecords ?? 0}";

                int currentRow = 5;

                // Add data based on report type
                switch (report.Filters?.ReportType?.ToLower())
                {
                    case "student":
                        AddStudentDataToExcel(worksheet, report.Data as StudentReportDTO, ref currentRow);
                        break;
                    case "tutor":
                        AddTutorDataToExcel(worksheet, report.Data as TutorReportDTO, ref currentRow);
                        break;
                    case "booking":
                        AddBookingDataToExcel(worksheet, report.Data as BookingReportDTO, ref currentRow);
                        break;
                    case "moduledemand":
                        AddModuleDemandDataToExcel(worksheet, report.Data as ModuleDemandReportDTO, ref currentRow);
                        break;
                    case "everything":
                        AddEverythingDataToExcel(worksheet, report.Data as EverythingReportDTO, ref currentRow);
                        break;
                    default:
                        worksheet.Cell(currentRow, 1).Value = "No data available for this report type";
                        break;
                }

                // Auto-fit columns for better readability
                worksheet.Columns().AdjustToContents();

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                var content = stream.ToArray();

                return File(content,
                           "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                           $"{report.ReportTitle?.Replace(" ", "_") ?? "report"}_{DateTime.UtcNow:yyyyMMdd}.xlsx");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error generating Excel report: {ex.Message}");
            }
        }

        // Excel Helper Methods
        private void AddStudentDataToExcel(IXLWorksheet worksheet, StudentReportDTO? data, ref int currentRow)
        {
            if (data?.Students == null || !data.Students.Any())
            {
                worksheet.Cell(currentRow, 1).Value = "No student data available";
                currentRow++;
                return;
            }

            // Headers
            worksheet.Cell(currentRow, 1).Value = "Student ID";
            worksheet.Cell(currentRow, 2).Value = "Name";
            worksheet.Cell(currentRow, 3).Value = "Email";
            worksheet.Cell(currentRow, 4).Value = "Total Bookings";
            worksheet.Cell(currentRow, 5).Value = "Completed Sessions";
            worksheet.Cell(currentRow, 6).Value = "Cancelled Sessions";
            worksheet.Cell(currentRow, 7).Value = "Engagement Score";
            worksheet.Cell(currentRow, 8).Value = "Status";

            // Style headers
            var headerRange = worksheet.Range(currentRow, 1, currentRow, 8);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            currentRow++;

            // Data rows
            foreach (var student in data.Students)
            {
                worksheet.Cell(currentRow, 1).Value = student.StudentId;
                worksheet.Cell(currentRow, 2).Value = student.Name ?? "";
                worksheet.Cell(currentRow, 3).Value = student.Email ?? "";
                worksheet.Cell(currentRow, 4).Value = student.TotalBookings;
                worksheet.Cell(currentRow, 5).Value = student.CompletedSessions;
                worksheet.Cell(currentRow, 6).Value = student.CancelledSessions;
                worksheet.Cell(currentRow, 7).Value = student.EngagementScore;
                worksheet.Cell(currentRow, 8).Value = student.IsActive ? "Active" : "Inactive";
                currentRow++;
            }

            // Add summary
            if (currentRow < 50) // Only add summary if there's space
            {
                worksheet.Cell(currentRow, 1).Value = "SUMMARY";
                worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
                currentRow++;
                worksheet.Cell(currentRow, 1).Value = "Total Registrations:";
                worksheet.Cell(currentRow, 2).Value = data.TotalRegistrations;
                currentRow++;
                worksheet.Cell(currentRow, 1).Value = "Total Bookings:";
                worksheet.Cell(currentRow, 2).Value = data.TotalBookings;
                currentRow++;
                worksheet.Cell(currentRow, 1).Value = "Average Sessions per Student:";
                worksheet.Cell(currentRow, 2).Value = Math.Round(data.AverageSessionsPerStudent, 2);
            }
        }

        private void AddTutorDataToExcel(IXLWorksheet worksheet, TutorReportDTO? data, ref int currentRow)
        {
            if (data?.Tutors == null || !data.Tutors.Any())
            {
                worksheet.Cell(currentRow, 1).Value = "No tutor data available";
                currentRow++;
                return;
            }

            // Headers
            worksheet.Cell(currentRow, 1).Value = "Tutor ID";
            worksheet.Cell(currentRow, 2).Value = "Name";
            worksheet.Cell(currentRow, 3).Value = "Email";
            worksheet.Cell(currentRow, 4).Value = "Modules";
            worksheet.Cell(currentRow, 5).Value = "Average Rating";
            worksheet.Cell(currentRow, 6).Value = "Total Sessions";
            worksheet.Cell(currentRow, 7).Value = "Completed Sessions";
            worksheet.Cell(currentRow, 8).Value = "Status";

            var headerRange = worksheet.Range(currentRow, 1, currentRow, 8);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            currentRow++;

            foreach (var tutor in data.Tutors)
            {
                worksheet.Cell(currentRow, 1).Value = tutor.TutorId;
                worksheet.Cell(currentRow, 2).Value = tutor.Name ?? "";
                worksheet.Cell(currentRow, 3).Value = tutor.Email ?? "";
                worksheet.Cell(currentRow, 4).Value = string.Join(", ", tutor.Modules ?? new List<string>());
                worksheet.Cell(currentRow, 5).Value = Math.Round(tutor.AverageRating, 2);
                worksheet.Cell(currentRow, 6).Value = tutor.TotalSessions;
                worksheet.Cell(currentRow, 7).Value = tutor.CompletedSessions;
                worksheet.Cell(currentRow, 8).Value = tutor.IsActive ? "Active" : "Inactive";
                currentRow++;
            }

            // Add summary
            if (currentRow < 50)
            {
                worksheet.Cell(currentRow, 1).Value = "SUMMARY";
                worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
                currentRow++;
                worksheet.Cell(currentRow, 1).Value = "Active Tutors:";
                worksheet.Cell(currentRow, 2).Value = data.ActiveTutors;
                currentRow++;
                worksheet.Cell(currentRow, 1).Value = "Average Rating:";
                worksheet.Cell(currentRow, 2).Value = Math.Round(data.AverageRating, 2);
                currentRow++;
                worksheet.Cell(currentRow, 1).Value = "Total Completed Sessions:";
                worksheet.Cell(currentRow, 2).Value = data.TotalCompletedSessions;
            }
        }

        private void AddBookingDataToExcel(IXLWorksheet worksheet, BookingReportDTO? data, ref int currentRow)
        {
            if (data?.Bookings == null || !data.Bookings.Any())
            {
                worksheet.Cell(currentRow, 1).Value = "No booking data available";
                currentRow++;
                return;
            }

            // Headers
            worksheet.Cell(currentRow, 1).Value = "Booking ID";
            worksheet.Cell(currentRow, 2).Value = "Student Name";
            worksheet.Cell(currentRow, 3).Value = "Tutor Name";
            worksheet.Cell(currentRow, 4).Value = "Module";
            worksheet.Cell(currentRow, 5).Value = "Session Date";
            worksheet.Cell(currentRow, 6).Value = "Duration (hours)";
            worksheet.Cell(currentRow, 7).Value = "Status";

            var headerRange = worksheet.Range(currentRow, 1, currentRow, 7);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            currentRow++;

            foreach (var booking in data.Bookings)
            {
                worksheet.Cell(currentRow, 1).Value = booking.BookingId;
                worksheet.Cell(currentRow, 2).Value = booking.StudentName ?? "";
                worksheet.Cell(currentRow, 3).Value = booking.TutorName ?? "";
                worksheet.Cell(currentRow, 4).Value = booking.Module ?? "N/A";
                worksheet.Cell(currentRow, 5).Value = booking.SessionDate.ToString("yyyy-MM-dd HH:mm");
                worksheet.Cell(currentRow, 6).Value = booking.Duration;
                worksheet.Cell(currentRow, 7).Value = booking.Status ?? "";
                currentRow++;
            }

            // Add summary
            if (currentRow < 50)
            {
                currentRow++;
                worksheet.Cell(currentRow, 1).Value = "BOOKING SUMMARY";
                worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
                currentRow++;
                worksheet.Cell(currentRow, 1).Value = "Pending:";
                worksheet.Cell(currentRow, 2).Value = data.PendingCount;
                currentRow++;
                worksheet.Cell(currentRow, 1).Value = "Confirmed:";
                worksheet.Cell(currentRow, 2).Value = data.ConfirmedCount;
                currentRow++;
                worksheet.Cell(currentRow, 1).Value = "Completed:";
                worksheet.Cell(currentRow, 2).Value = data.CompletedCount;
                currentRow++;
                worksheet.Cell(currentRow, 1).Value = "Cancelled:";
                worksheet.Cell(currentRow, 2).Value = data.CancelledCount;
            }
        }

        private void AddModuleDemandDataToExcel(IXLWorksheet worksheet, ModuleDemandReportDTO? data, ref int currentRow)
        {
            if (data?.Modules == null || !data.Modules.Any())
            {
                worksheet.Cell(currentRow, 1).Value = "No module data available";
                currentRow++;
                return;
            }

            // Headers
            worksheet.Cell(currentRow, 1).Value = "Module Code";
            worksheet.Cell(currentRow, 2).Value = "Module Name";
            worksheet.Cell(currentRow, 3).Value = "Total Bookings";
            worksheet.Cell(currentRow, 4).Value = "Unique Tutors";
            worksheet.Cell(currentRow, 5).Value = "Unique Students";
            worksheet.Cell(currentRow, 6).Value = "Growth Rate";

            var headerRange = worksheet.Range(currentRow, 1, currentRow, 6);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            currentRow++;

            foreach (var module in data.Modules)
            {
                worksheet.Cell(currentRow, 1).Value = module.ModuleCode ?? "";
                worksheet.Cell(currentRow, 2).Value = module.ModuleName ?? "";
                worksheet.Cell(currentRow, 3).Value = module.TotalBookings;
                worksheet.Cell(currentRow, 4).Value = module.UniqueTutors;
                worksheet.Cell(currentRow, 5).Value = module.UniqueStudents;
                worksheet.Cell(currentRow, 6).Value = Math.Round(module.GrowthRate, 2);
                currentRow++;
            }

            // Add summary
            if (currentRow < 50)
            {
                currentRow++;
                worksheet.Cell(currentRow, 1).Value = "SUMMARY";
                worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
                currentRow++;
                worksheet.Cell(currentRow, 1).Value = "Total Bookings:";
                worksheet.Cell(currentRow, 2).Value = data.TotalBookings;
                currentRow++;
                worksheet.Cell(currentRow, 1).Value = "Most Popular:";
                worksheet.Cell(currentRow, 2).Value = data.MostPopularModule ?? "N/A";
                currentRow++;
                worksheet.Cell(currentRow, 1).Value = "Least Popular:";
                worksheet.Cell(currentRow, 2).Value = data.LeastPopularModule ?? "N/A";
            }
        }

        private void AddEverythingDataToExcel(IXLWorksheet worksheet, EverythingReportDTO? data, ref int currentRow)
        {
            if (data == null)
            {
                worksheet.Cell(currentRow, 1).Value = "No data available for comprehensive report";
                currentRow++;
                return;
            }

            // Add Overall Summary Section
            worksheet.Cell(currentRow, 1).Value = "OVERALL SYSTEM SUMMARY";
            worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
            worksheet.Cell(currentRow, 1).Style.Font.FontSize = 14;
            currentRow++;

            // Overall Stats Table
            worksheet.Cell(currentRow, 1).Value = "Metric";
            worksheet.Cell(currentRow, 2).Value = "Value";
            var headerRange = worksheet.Range(currentRow, 1, currentRow, 2);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            currentRow++;

            var overallStats = data.OverallStats;
            AddSummaryRowToExcel(worksheet, "Total Students", overallStats.TotalStudents.ToString(), ref currentRow);
            AddSummaryRowToExcel(worksheet, "Total Tutors", overallStats.TotalTutors.ToString(), ref currentRow);
            AddSummaryRowToExcel(worksheet, "Total Bookings", overallStats.TotalBookings.ToString(), ref currentRow);
            AddSummaryRowToExcel(worksheet, "Total Modules", overallStats.TotalModules.ToString(), ref currentRow);
            AddSummaryRowToExcel(worksheet, "Active Users", overallStats.ActiveUsers.ToString(), ref currentRow);
            AddSummaryRowToExcel(worksheet, "Average Tutor Rating", overallStats.AverageRating.ToString("F2"), ref currentRow);

            currentRow += 2;

            // Student Data Section
            if (data.StudentReport?.Students != null && data.StudentReport.Students.Any())
            {
                worksheet.Cell(currentRow, 1).Value = "STUDENT DATA SUMMARY";
                worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
                currentRow++;

                worksheet.Cell(currentRow, 1).Value = "Total Students";
                worksheet.Cell(currentRow, 2).Value = data.StudentReport.TotalRegistrations;
                currentRow++;
                worksheet.Cell(currentRow, 1).Value = "Total Bookings by Students";
                worksheet.Cell(currentRow, 2).Value = data.StudentReport.TotalBookings;
                currentRow++;
                worksheet.Cell(currentRow, 1).Value = "Average Sessions per Student";
                worksheet.Cell(currentRow, 2).Value = Math.Round(data.StudentReport.AverageSessionsPerStudent, 2);
                currentRow++;

                currentRow += 2;
            }

            // Tutor Data Section
            if (data.TutorReport?.Tutors != null && data.TutorReport.Tutors.Any())
            {
                worksheet.Cell(currentRow, 1).Value = "TUTOR DATA SUMMARY";
                worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
                currentRow++;

                worksheet.Cell(currentRow, 1).Value = "Total Tutors";
                worksheet.Cell(currentRow, 2).Value = data.TutorReport.Tutors.Count;
                currentRow++;
                worksheet.Cell(currentRow, 1).Value = "Active Tutors";
                worksheet.Cell(currentRow, 2).Value = data.TutorReport.ActiveTutors;
                currentRow++;
                worksheet.Cell(currentRow, 1).Value = "Average Rating";
                worksheet.Cell(currentRow, 2).Value = Math.Round(data.TutorReport.AverageRating, 2);
                currentRow++;
                worksheet.Cell(currentRow, 1).Value = "Total Completed Sessions";
                worksheet.Cell(currentRow, 2).Value = data.TutorReport.TotalCompletedSessions;
                currentRow++;

                currentRow += 2;
            }

            // Booking Data Section
            if (data.BookingReport?.Bookings != null && data.BookingReport.Bookings.Any())
            {
                worksheet.Cell(currentRow, 1).Value = "BOOKING DATA SUMMARY";
                worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
                currentRow++;

                worksheet.Cell(currentRow, 1).Value = "Pending Bookings";
                worksheet.Cell(currentRow, 2).Value = data.BookingReport.PendingCount;
                currentRow++;
                worksheet.Cell(currentRow, 1).Value = "Confirmed Bookings";
                worksheet.Cell(currentRow, 2).Value = data.BookingReport.ConfirmedCount;
                currentRow++;
                worksheet.Cell(currentRow, 1).Value = "Completed Bookings";
                worksheet.Cell(currentRow, 2).Value = data.BookingReport.CompletedCount;
                currentRow++;
                worksheet.Cell(currentRow, 1).Value = "Cancelled Bookings";
                worksheet.Cell(currentRow, 2).Value = data.BookingReport.CancelledCount;
                currentRow++;

                currentRow += 2;
            }

            // Module Demand Section
            if (data.ModuleDemandReport?.Modules != null && data.ModuleDemandReport.Modules.Any())
            {
                worksheet.Cell(currentRow, 1).Value = "MODULE DEMAND SUMMARY";
                worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
                currentRow++;

                worksheet.Cell(currentRow, 1).Value = "Total Bookings Across Modules";
                worksheet.Cell(currentRow, 2).Value = data.ModuleDemandReport.TotalBookings;
                currentRow++;
                worksheet.Cell(currentRow, 1).Value = "Most Popular Module";
                worksheet.Cell(currentRow, 2).Value = data.ModuleDemandReport.MostPopularModule ?? "N/A";
                currentRow++;
                worksheet.Cell(currentRow, 1).Value = "Least Popular Module";
                worksheet.Cell(currentRow, 2).Value = data.ModuleDemandReport.LeastPopularModule ?? "N/A";
                currentRow++;

                // Top 5 Modules by Demand
                currentRow++;
                worksheet.Cell(currentRow, 1).Value = "TOP 5 MODULES BY DEMAND";
                worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
                currentRow++;

                worksheet.Cell(currentRow, 1).Value = "Module Name";
                worksheet.Cell(currentRow, 2).Value = "Total Bookings";
                worksheet.Cell(currentRow, 3).Value = "Unique Tutors";
                worksheet.Cell(currentRow, 4).Value = "Unique Students";
                headerRange = worksheet.Range(currentRow, 1, currentRow, 4);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
                currentRow++;

                var topModules = data.ModuleDemandReport.Modules.Take(5);
                foreach (var module in topModules)
                {
                    worksheet.Cell(currentRow, 1).Value = module.ModuleName ?? "";
                    worksheet.Cell(currentRow, 2).Value = module.TotalBookings;
                    worksheet.Cell(currentRow, 3).Value = module.UniqueTutors;
                    worksheet.Cell(currentRow, 4).Value = module.UniqueStudents;
                    currentRow++;
                }
            }
        }

        private void AddSummaryRowToExcel(IXLWorksheet worksheet, string label, string value, ref int currentRow)
        {
            worksheet.Cell(currentRow, 1).Value = label;
            worksheet.Cell(currentRow, 2).Value = value;
            currentRow++;
        }


        //PDF REPORT GENERATOR
        private IActionResult GeneratePdfReport(ReportResultDTO report)
        {
            try
            {
                using var stream = new MemoryStream();

                // Create document with proper margins
                var document = new Document(PageSize.A4, 40, 40, 60, 40);
                var writer = PdfWriter.GetInstance(document, stream);

                document.Open();

                // Add logo
                AddLogo(document);

                // Add main title with styling
                AddTitleSection(document, report.ReportTitle ?? "Report");

                // Add report metadata
                AddMetadataSection(document, report);

                // Add filters summary
                AddFiltersSection(document, report.Filters);

                // Add data based on report type
                switch (report.Filters?.ReportType?.ToLower())
                {
                    case "student":
                        AddStudentDataToPdf(document, report.Data as StudentReportDTO, report.Summary);
                        break;
                    case "tutor":
                        AddTutorDataToPdf(document, report.Data as TutorReportDTO, report.Summary);
                        break;
                    case "booking":
                        AddBookingDataToPdf(document, report.Data as BookingReportDTO, report.Summary);
                        break;
                    case "moduledemand":
                        AddModuleDemandDataToPdf(document, report.Data as ModuleDemandReportDTO, report.Summary);
                        break;
                    case "everything":
                        AddEverythingDataToPdf(document, report.Data as EverythingReportDTO, report.Summary);
                        break;
                    default:
                        AddNoDataMessage(document);
                        break;
                }

                // Add footer manually (simpler approach)
                AddFooter(document, report.GeneratedAt);

                document.Close();

                return File(stream.ToArray(), "application/pdf",
                           $"{report.ReportTitle?.Replace(" ", "_") ?? "report"}_{DateTime.UtcNow:yyyyMMdd}.pdf");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error generating PDF report: {ex.Message}");
            }
        }

        // Add footer manually instead of using page events
        private void AddFooter(Document document, DateTime generatedAt)
        {
            document.Add(new Paragraph(" "));
            document.Add(new Paragraph(" "));

            var footerLine = new Paragraph(new Chunk(
                new iTextSharp.text.pdf.draw.LineSeparator(1f, 100f, BaseColor.LIGHT_GRAY, Element.ALIGN_CENTER, 0)));
            document.Add(footerLine);

            var footerText = new Paragraph($"Generated on {generatedAt:yyyy-MM-dd HH:mm} UTC",
                new Font(FontFactory.GetFont(FontFactory.HELVETICA, 8, BaseColor.DARK_GRAY)));
            footerText.Alignment = Element.ALIGN_CENTER;
            document.Add(footerText);
        }



        // Brand colors matching common web app themes
        private static class BrandColors
        {
            public static BaseColor Primary = new BaseColor(79, 70, 229);    // Indigo
            public static BaseColor Secondary = new BaseColor(99, 102, 241); // Lighter Indigo
            public static BaseColor Success = new BaseColor(16, 185, 129);   // Green
            public static BaseColor Warning = new BaseColor(245, 158, 11);   // Amber
            public static BaseColor Danger = new BaseColor(239, 68, 68);     // Red
            public static BaseColor Dark = new BaseColor(31, 41, 55);        // Gray-800
            public static BaseColor Light = new BaseColor(249, 250, 251);    // Gray-50
            public static BaseColor Border = new BaseColor(229, 231, 235);   // Gray-200
        }

        private void AddLogo(Document document)
        {
            try
            {
                // Try to load your logo - adjust the path as needed
                var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "logo.jpg");
                if (System.IO.File.Exists(logoPath))
                {
                    var logo = Image.GetInstance(logoPath);
                    logo.ScaleToFit(150, 60);
                    logo.Alignment = Image.ALIGN_LEFT;
                    document.Add(logo);
                }
                else
                {
                    // Fallback: Add text logo
                    var textLogo = new Paragraph("TutorConnect",
                        new Font(FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 20, BrandColors.Primary)));
                    textLogo.Alignment = Element.ALIGN_LEFT;
                    document.Add(textLogo);
                }

                document.Add(new Paragraph(" ")); // Spacing
            }
            catch (Exception)
            {
                // Silently continue if logo can't be loaded
            }
        }

        private void AddTitleSection(Document document, string title)
        {
            var titleParagraph = new Paragraph(title.ToUpper(),
                new Font(FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 24, BrandColors.Primary)));
            titleParagraph.Alignment = Element.ALIGN_CENTER;
            titleParagraph.SpacingAfter = 20f;
            document.Add(titleParagraph);
        }

        private void AddMetadataSection(Document document, ReportResultDTO report)
        {
            var metadataTable = new PdfPTable(2);
            metadataTable.WidthPercentage = 100;
            metadataTable.SetWidths(new float[] { 30, 70 });
            metadataTable.SpacingBefore = 10f;
            metadataTable.SpacingAfter = 15f;

            AddMetadataRow(metadataTable, "Generated:", report.GeneratedAt.ToString("yyyy-MM-dd HH:mm UTC"));
            AddMetadataRow(metadataTable, "Total Records:", (report.Summary?.TotalRecords ?? 0).ToString());

            if (report.Summary?.ActiveUsers > 0)
                AddMetadataRow(metadataTable, "Active Users:", report.Summary.ActiveUsers.ToString());

            if (report.Summary?.AverageRating > 0)
                AddMetadataRow(metadataTable, "Average Rating:", Math.Round(report.Summary.AverageRating, 2).ToString());

            document.Add(metadataTable);
        }

        private void AddMetadataRow(PdfPTable table, string label, string value)
        {
            var labelCell = new PdfPCell(new Phrase(label,
                new Font(FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, BaseColor.DARK_GRAY))));
            labelCell.Border = Rectangle.NO_BORDER;
            labelCell.Padding = 5;
            table.AddCell(labelCell);

            var valueCell = new PdfPCell(new Phrase(value,
                new Font(FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.BLACK))));
            valueCell.Border = Rectangle.NO_BORDER;
            valueCell.Padding = 5;
            table.AddCell(valueCell);
        }

        private void AddFiltersSection(Document document, ReportFilterDTO filters)
        {
            if (filters == null) return;

            var filtersHeader = new Paragraph("APPLIED FILTERS",
                new Font(FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, BrandColors.Dark)));
            filtersHeader.SpacingBefore = 15f;
            filtersHeader.SpacingAfter = 8f;
            document.Add(filtersHeader);

            var filtersTable = new PdfPTable(2);
            filtersTable.WidthPercentage = 100;
            filtersTable.SetWidths(new float[] { 30, 70 });
            filtersTable.DefaultCell.Border = Rectangle.NO_BORDER;

            if (!string.IsNullOrEmpty(filters.ReportType))
                AddFilterRow(filtersTable, "Report Type:", filters.ReportType);

            if (!string.IsNullOrEmpty(filters.DateRange))
                AddFilterRow(filtersTable, "Date Range:", filters.DateRange);

            if (filters.StartDate.HasValue && filters.EndDate.HasValue)
                AddFilterRow(filtersTable, "Custom Dates:", $"{filters.StartDate:yyyy-MM-dd} to {filters.EndDate:yyyy-MM-dd}");

            if (!string.IsNullOrEmpty(filters.UserType) && filters.UserId.HasValue)
                AddFilterRow(filtersTable, "User Filter:", $"{filters.UserType} (ID: {filters.UserId})");

            if (filters.Statuses != null && filters.Statuses.Any())
                AddFilterRow(filtersTable, "Statuses:", string.Join(", ", filters.Statuses));

            if (filters.ModuleIds != null && filters.ModuleIds.Any())
                AddFilterRow(filtersTable, "Modules:", $"{filters.ModuleIds.Count} selected");

            if (filtersTable.Rows.Count > 0)
                document.Add(filtersTable);

            document.Add(new Paragraph(" "));
        }

        private void AddFilterRow(PdfPTable table, string label, string value)
        {
            var labelCell = new PdfPCell(new Phrase(label,
                new Font(FontFactory.GetFont(FontFactory.HELVETICA, 9, BaseColor.DARK_GRAY))));
            labelCell.Border = Rectangle.NO_BORDER;
            labelCell.Padding = 3;
            table.AddCell(labelCell);

            var valueCell = new PdfPCell(new Phrase(value,
                new Font(FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9, BrandColors.Primary))));
            valueCell.Border = Rectangle.NO_BORDER;
            valueCell.Padding = 3;
            table.AddCell(valueCell);
        }

        // Enhanced PDF Helper Methods with better styling
        private void AddStudentDataToPdf(Document document, StudentReportDTO? data, SummaryDTO? summary)
        {
            if (data?.Students == null || !data.Students.Any())
            {
                AddNoDataMessage(document);
                return;
            }

            var sectionHeader = new Paragraph("STUDENT DATA",
                new Font(FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14, BrandColors.Primary)));
            sectionHeader.SpacingBefore = 20f;
            sectionHeader.SpacingAfter = 10f;
            document.Add(sectionHeader);

            var table = new PdfPTable(7);
            table.WidthPercentage = 100;
            table.SetWidths(new float[] { 10, 15, 25, 12, 12, 12, 14 });
            table.SpacingBefore = 10f;

            // Add styled headers
            AddTableHeader(table, "Student ID");
            AddTableHeader(table, "Name");
            AddTableHeader(table, "Email");
            AddTableHeader(table, "Total Bookings");
            AddTableHeader(table, "Completed");
            AddTableHeader(table, "Cancelled");
            AddTableHeader(table, "Status");

            // Add data rows with alternating colors
            var rowIndex = 0;
            foreach (var student in data.Students)
            {
                var bgColor = rowIndex % 2 == 0 ? BaseColor.WHITE : new BaseColor(249, 250, 251);

                AddTableDataCell(table, student.StudentId.ToString(), bgColor);
                AddTableDataCell(table, student.Name ?? "", bgColor);
                AddTableDataCell(table, student.Email ?? "", bgColor);
                AddTableDataCell(table, student.TotalBookings.ToString(), bgColor);
                AddTableDataCell(table, student.CompletedSessions.ToString(), bgColor);
                AddTableDataCell(table, student.CancelledSessions.ToString(), bgColor);

                var statusCell = new PdfPCell(new Phrase(student.IsActive ? "Active" : "Inactive",
                    new Font(FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9,
                        student.IsActive ? BrandColors.Success : BrandColors.Danger))));
                statusCell.BackgroundColor = bgColor;
                statusCell.Padding = 5;
                statusCell.BorderWidth = 0.5f;
                statusCell.BorderColor = BrandColors.Border;
                table.AddCell(statusCell);

                rowIndex++;
            }

            document.Add(table);
        }

        private void AddTutorDataToPdf(Document document, TutorReportDTO? data, SummaryDTO? summary)
        {
            if (data?.Tutors == null || !data.Tutors.Any())
            {
                AddNoDataMessage(document);
                return;
            }

            var sectionHeader = new Paragraph("TUTOR DATA",
                new Font(FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14, BrandColors.Primary)));
            sectionHeader.SpacingBefore = 20f;
            sectionHeader.SpacingAfter = 10f;
            document.Add(sectionHeader);

            var table = new PdfPTable(7);
            table.WidthPercentage = 100;
            table.SetWidths(new float[] { 10, 15, 22, 20, 12, 12, 9 });
            table.SpacingBefore = 10f;

            AddTableHeader(table, "Tutor ID");
            AddTableHeader(table, "Name");
            AddTableHeader(table, "Email");
            AddTableHeader(table, "Modules");
            AddTableHeader(table, "Avg Rating");
            AddTableHeader(table, "Sessions");
            AddTableHeader(table, "Status");

            var rowIndex = 0;
            foreach (var tutor in data.Tutors)
            {
                var bgColor = rowIndex % 2 == 0 ? BaseColor.WHITE : new BaseColor(249, 250, 251);

                AddTableDataCell(table, tutor.TutorId.ToString(), bgColor);
                AddTableDataCell(table, tutor.Name ?? "", bgColor);
                AddTableDataCell(table, tutor.Email ?? "", bgColor);
                AddTableDataCell(table, string.Join(", ", tutor.Modules?.Take(2) ?? new List<string>()), bgColor);
                AddTableDataCell(table, Math.Round(tutor.AverageRating, 1).ToString(), bgColor);
                AddTableDataCell(table, tutor.TotalSessions.ToString(), bgColor);

                var statusCell = new PdfPCell(new Phrase(tutor.IsActive ? "Active" : "Inactive",
                    new Font(FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9,
                        tutor.IsActive ? BrandColors.Success : BrandColors.Danger))));
                statusCell.BackgroundColor = bgColor;
                statusCell.Padding = 5;
                statusCell.BorderWidth = 0.5f;
                statusCell.BorderColor = BrandColors.Border;
                table.AddCell(statusCell);

                rowIndex++;
            }

            document.Add(table);
        }

        private void AddBookingDataToPdf(Document document, BookingReportDTO? data, SummaryDTO? summary)
        {
            if (data?.Bookings == null || !data.Bookings.Any())
            {
                AddNoDataMessage(document);
                return;
            }

            var sectionHeader = new Paragraph("BOOKING DATA",
                new Font(FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14, BrandColors.Primary)));
            sectionHeader.SpacingBefore = 20f;
            sectionHeader.SpacingAfter = 10f;
            document.Add(sectionHeader);

            var table = new PdfPTable(6);
            table.WidthPercentage = 100;
            table.SetWidths(new float[] { 12, 18, 18, 20, 18, 14 });
            table.SpacingBefore = 10f;

            AddTableHeader(table, "Booking ID");
            AddTableHeader(table, "Student");
            AddTableHeader(table, "Tutor");
            AddTableHeader(table, "Module");
            AddTableHeader(table, "Session Date");
            AddTableHeader(table, "Status");

            var rowIndex = 0;
            foreach (var booking in data.Bookings.Take(100)) // Increased limit for better reports
            {
                var bgColor = rowIndex % 2 == 0 ? BaseColor.WHITE : new BaseColor(249, 250, 251);

                AddTableDataCell(table, booking.BookingId.ToString(), bgColor);
                AddTableDataCell(table, booking.StudentName ?? "", bgColor);
                AddTableDataCell(table, booking.TutorName ?? "", bgColor);
                AddTableDataCell(table, booking.Module ?? "N/A", bgColor);
                AddTableDataCell(table, booking.SessionDate.ToString("MMM dd, yyyy HH:mm"), bgColor);

                var statusColor = booking.Status?.ToLower() switch
                {
                    "completed" => BrandColors.Success,
                    "confirmed" => BrandColors.Secondary,
                    "pending" => BrandColors.Warning,
                    "cancelled" => BrandColors.Danger,
                    _ => BaseColor.DARK_GRAY
                };

                var statusCell = new PdfPCell(new Phrase(booking.Status ?? "",
                    new Font(FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9, statusColor))));
                statusCell.BackgroundColor = bgColor;
                statusCell.Padding = 5;
                statusCell.BorderWidth = 0.5f;
                statusCell.BorderColor = BrandColors.Border;
                table.AddCell(statusCell);

                rowIndex++;
            }

            document.Add(table);
        }

        private void AddModuleDemandDataToPdf(Document document, ModuleDemandReportDTO? data, SummaryDTO? summary)
        {
            if (data?.Modules == null || !data.Modules.Any())
            {
                AddNoDataMessage(document);
                return;
            }

            var sectionHeader = new Paragraph("MODULE DEMAND DATA",
                new Font(FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14, BrandColors.Primary)));
            sectionHeader.SpacingBefore = 20f;
            sectionHeader.SpacingAfter = 10f;
            document.Add(sectionHeader);

            var table = new PdfPTable(5);
            table.WidthPercentage = 100;
            table.SetWidths(new float[] { 20, 30, 15, 15, 20 });
            table.SpacingBefore = 10f;

            AddTableHeader(table, "Module Code");
            AddTableHeader(table, "Module Name");
            AddTableHeader(table, "Total Bookings");
            AddTableHeader(table, "Unique Tutors");
            AddTableHeader(table, "Unique Students");

            var rowIndex = 0;
            foreach (var module in data.Modules)
            {
                var bgColor = rowIndex % 2 == 0 ? BaseColor.WHITE : new BaseColor(249, 250, 251);

                AddTableDataCell(table, module.ModuleCode ?? "", bgColor);
                AddTableDataCell(table, module.ModuleName ?? "", bgColor);
                AddTableDataCell(table, module.TotalBookings.ToString(), bgColor);
                AddTableDataCell(table, module.UniqueTutors.ToString(), bgColor);
                AddTableDataCell(table, module.UniqueStudents.ToString(), bgColor);

                rowIndex++;
            }

            document.Add(table);
        }


        private void AddEverythingDataToPdf(Document document, EverythingReportDTO? data, SummaryDTO? summary)
        {
            if (data == null)
            {
                AddNoDataMessage(document);
                return;
            }

            // Overall Summary Section
            var overallHeader = new Paragraph("OVERALL SYSTEM SUMMARY",
                new Font(FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16, BrandColors.Primary)));
            overallHeader.SpacingBefore = 20f;
            overallHeader.SpacingAfter = 15f;
            document.Add(overallHeader);

            // Overall Stats Table
            var overallTable = new PdfPTable(2);
            overallTable.WidthPercentage = 80;
            overallTable.HorizontalAlignment = Element.ALIGN_CENTER;
            overallTable.SetWidths(new float[] { 60, 40 });

            var overallStats = data.OverallStats;
            AddSummaryRowToPdf(overallTable, "Total Students", overallStats.TotalStudents.ToString());
            AddSummaryRowToPdf(overallTable, "Total Tutors", overallStats.TotalTutors.ToString());
            AddSummaryRowToPdf(overallTable, "Total Bookings", overallStats.TotalBookings.ToString());
            AddSummaryRowToPdf(overallTable, "Total Modules", overallStats.TotalModules.ToString());
            AddSummaryRowToPdf(overallTable, "Active Users", overallStats.ActiveUsers.ToString());
            AddSummaryRowToPdf(overallTable, "Average Tutor Rating", overallStats.AverageRating.ToString("F2"));

            document.Add(overallTable);

            // Student Data Section
            if (data.StudentReport?.Students != null && data.StudentReport.Students.Any())
            {
                document.Add(new Paragraph(" "));
                var studentHeader = new Paragraph("STUDENT DATA SUMMARY",
                    new Font(FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14, BrandColors.Primary)));
                studentHeader.SpacingBefore = 20f;
                studentHeader.SpacingAfter = 10f;
                document.Add(studentHeader);

                var studentTable = new PdfPTable(2);
                studentTable.WidthPercentage = 60;
                studentTable.HorizontalAlignment = Element.ALIGN_LEFT;

                AddSummaryRowToPdf(studentTable, "Total Students", data.StudentReport.TotalRegistrations.ToString());
                AddSummaryRowToPdf(studentTable, "Total Bookings", data.StudentReport.TotalBookings.ToString());
                AddSummaryRowToPdf(studentTable, "Avg Sessions/Student", Math.Round(data.StudentReport.AverageSessionsPerStudent, 2).ToString());

                document.Add(studentTable);
            }

            // Tutor Data Section
            if (data.TutorReport?.Tutors != null && data.TutorReport.Tutors.Any())
            {
                document.Add(new Paragraph(" "));
                var tutorHeader = new Paragraph("TUTOR DATA SUMMARY",
                    new Font(FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14, BrandColors.Primary)));
                tutorHeader.SpacingBefore = 20f;
                tutorHeader.SpacingAfter = 10f;
                document.Add(tutorHeader);

                var tutorTable = new PdfPTable(2);
                tutorTable.WidthPercentage = 60;
                tutorTable.HorizontalAlignment = Element.ALIGN_LEFT;

                AddSummaryRowToPdf(tutorTable, "Total Tutors", data.TutorReport.Tutors.Count.ToString());
                AddSummaryRowToPdf(tutorTable, "Active Tutors", data.TutorReport.ActiveTutors.ToString());
                AddSummaryRowToPdf(tutorTable, "Average Rating", Math.Round(data.TutorReport.AverageRating, 2).ToString());
                AddSummaryRowToPdf(tutorTable, "Completed Sessions", data.TutorReport.TotalCompletedSessions.ToString());

                document.Add(tutorTable);
            }

            // Booking Data Section
            if (data.BookingReport?.Bookings != null && data.BookingReport.Bookings.Any())
            {
                document.Add(new Paragraph(" "));
                var bookingHeader = new Paragraph("BOOKING DATA SUMMARY",
                    new Font(FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14, BrandColors.Primary)));
                bookingHeader.SpacingBefore = 20f;
                bookingHeader.SpacingAfter = 10f;
                document.Add(bookingHeader);

                var bookingTable = new PdfPTable(2);
                bookingTable.WidthPercentage = 60;
                bookingTable.HorizontalAlignment = Element.ALIGN_LEFT;

                AddSummaryRowToPdf(bookingTable, "Pending Bookings", data.BookingReport.PendingCount.ToString());
                AddSummaryRowToPdf(bookingTable, "Confirmed Bookings", data.BookingReport.ConfirmedCount.ToString());
                AddSummaryRowToPdf(bookingTable, "Completed Bookings", data.BookingReport.CompletedCount.ToString());
                AddSummaryRowToPdf(bookingTable, "Cancelled Bookings", data.BookingReport.CancelledCount.ToString());

                document.Add(bookingTable);
            }

            // Module Demand Section
            if (data.ModuleDemandReport?.Modules != null && data.ModuleDemandReport.Modules.Any())
            {
                document.Add(new Paragraph(" "));
                var moduleHeader = new Paragraph("MODULE DEMAND SUMMARY",
                    new Font(FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14, BrandColors.Primary)));
                moduleHeader.SpacingBefore = 20f;
                moduleHeader.SpacingAfter = 10f;
                document.Add(moduleHeader);

                var moduleTable = new PdfPTable(2);
                moduleTable.WidthPercentage = 60;
                moduleTable.HorizontalAlignment = Element.ALIGN_LEFT;

                AddSummaryRowToPdf(moduleTable, "Total Module Bookings", data.ModuleDemandReport.TotalBookings.ToString());
                AddSummaryRowToPdf(moduleTable, "Most Popular", data.ModuleDemandReport.MostPopularModule ?? "N/A");
                AddSummaryRowToPdf(moduleTable, "Least Popular", data.ModuleDemandReport.LeastPopularModule ?? "N/A");

                document.Add(moduleTable);

                // Top Modules Table
                document.Add(new Paragraph(" "));
                var topModulesHeader = new Paragraph("TOP 5 MODULES BY DEMAND",
                    new Font(FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, BrandColors.Dark)));
                topModulesHeader.SpacingBefore = 15f;
                topModulesHeader.SpacingAfter = 10f;
                document.Add(topModulesHeader);

                var topModulesTable = new PdfPTable(4);
                topModulesTable.WidthPercentage = 100;
                topModulesTable.SetWidths(new float[] { 40, 20, 20, 20 });

                AddTableHeader(topModulesTable, "Module Name");
                AddTableHeader(topModulesTable, "Total Bookings");
                AddTableHeader(topModulesTable, "Unique Tutors");
                AddTableHeader(topModulesTable, "Unique Students");

                var topModules = data.ModuleDemandReport.Modules.Take(5);
                var rowIndex = 0;
                foreach (var module in topModules)
                {
                    var bgColor = rowIndex % 2 == 0 ? BaseColor.WHITE : new BaseColor(249, 250, 251);
                    AddTableDataCell(topModulesTable, module.ModuleName ?? "", bgColor);
                    AddTableDataCell(topModulesTable, module.TotalBookings.ToString(), bgColor);
                    AddTableDataCell(topModulesTable, module.UniqueTutors.ToString(), bgColor);
                    AddTableDataCell(topModulesTable, module.UniqueStudents.ToString(), bgColor);
                    rowIndex++;
                }

                document.Add(topModulesTable);
            }
        }

        private void AddSummaryRowToPdf(PdfPTable table, string label, string value)
        {
            var labelCell = new PdfPCell(new Phrase(label,
                new Font(FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, BaseColor.DARK_GRAY))));
            labelCell.Border = Rectangle.NO_BORDER;
            labelCell.Padding = 4;
            table.AddCell(labelCell);

            var valueCell = new PdfPCell(new Phrase(value,
                new Font(FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, BrandColors.Primary))));
            valueCell.Border = Rectangle.NO_BORDER;
            valueCell.Padding = 4;
            table.AddCell(valueCell);
        }

        // Helper methods for consistent styling
        private void AddTableHeader(PdfPTable table, string text)
        {
            var cell = new PdfPCell(new Phrase(text,
                new Font(FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, BaseColor.WHITE))));
            cell.BackgroundColor = BrandColors.Primary;
            cell.Padding = 8;
            cell.BorderWidth = 0.5f;
            cell.BorderColor = BrandColors.Primary;
            cell.HorizontalAlignment = Element.ALIGN_CENTER;
            table.AddCell(cell);
        }

        private void AddTableDataCell(PdfPTable table, string text, BaseColor backgroundColor)
        {
            var cell = new PdfPCell(new Phrase(text,
                new Font(FontFactory.GetFont(FontFactory.HELVETICA, 9, BaseColor.DARK_GRAY))));
            cell.BackgroundColor = backgroundColor;
            cell.Padding = 6;
            cell.BorderWidth = 0.5f;
            cell.BorderColor = BrandColors.Border;
            table.AddCell(cell);
        }

        private void AddNoDataMessage(Document document)
        {
            var message = new Paragraph("No data available for the selected filters",
                new Font(FontFactory.GetFont(FontFactory.HELVETICA, 12, BaseColor.DARK_GRAY)));
            message.Alignment = Element.ALIGN_CENTER;
            message.SpacingBefore = 50f;
            document.Add(message);
        }

        private void AddSummarySection(Document document, SummaryDTO? summary)
        {
            if (summary == null) return;

            var summaryHeader = new Paragraph("REPORT SUMMARY",
                new Font(FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, BrandColors.Dark)));
            summaryHeader.SpacingBefore = 30f;
            summaryHeader.SpacingAfter = 10f;
            document.Add(summaryHeader);

            var summaryTable = new PdfPTable(2);
            summaryTable.WidthPercentage = 50;
            summaryTable.HorizontalAlignment = Element.ALIGN_LEFT;

            if (summary.TotalRecords > 0)
                AddSummaryRow(summaryTable, "Total Records:", summary.TotalRecords.ToString());

            if (summary.ActiveUsers > 0)
                AddSummaryRow(summaryTable, "Active Users:", summary.ActiveUsers.ToString());

            if (summary.AverageRating > 0)
                AddSummaryRow(summaryTable, "Average Rating:", Math.Round(summary.AverageRating, 2).ToString());

            if (summary.TotalRevenue > 0)
                AddSummaryRow(summaryTable, "Total Revenue:", $"${summary.TotalRevenue:F2}");

            document.Add(summaryTable);
        }

        private void AddSummaryRow(PdfPTable table, string label, string value)
        {
            var labelCell = new PdfPCell(new Phrase(label,
                new Font(FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, BaseColor.DARK_GRAY))));
            labelCell.Border = Rectangle.NO_BORDER;
            labelCell.Padding = 4;
            table.AddCell(labelCell);

            var valueCell = new PdfPCell(new Phrase(value,
                new Font(FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, BrandColors.Primary))));
            valueCell.Border = Rectangle.NO_BORDER;
            valueCell.Padding = 4;
            table.AddCell(valueCell);
        }

        private IActionResult GenerateCsvReport(ReportResultDTO report)
        {
            try
            {
                var csvBuilder = new StringBuilder();

                // Add header
                csvBuilder.AppendLine($"Report Title: {report.ReportTitle}");
                csvBuilder.AppendLine($"Generated on: {report.GeneratedAt:yyyy-MM-dd HH:mm}");
                csvBuilder.AppendLine($"Total Records: {report.Summary?.TotalRecords ?? 0}");
                csvBuilder.AppendLine();

                // Add data based on report type
                switch (report.Filters?.ReportType?.ToLower())
                {
                    case "student":
                        AddStudentDataToCsv(csvBuilder, report.Data as StudentReportDTO);
                        break;
                    case "tutor":
                        AddTutorDataToCsv(csvBuilder, report.Data as TutorReportDTO);
                        break;
                    case "booking":
                        AddBookingDataToCsv(csvBuilder, report.Data as BookingReportDTO);
                        break;
                    case "moduledemand":
                        AddModuleDemandDataToCsv(csvBuilder, report.Data as ModuleDemandReportDTO);
                        break;
                    case "everything":
                        AddEverythingDataToCsv(csvBuilder, report.Data as EverythingReportDTO);
                        break;
                    default:
                        csvBuilder.AppendLine("No data available for this report type");
                        break;
                }

                var content = Encoding.UTF8.GetBytes(csvBuilder.ToString());
                return File(content, "text/csv",
                           $"{report.ReportTitle?.Replace(" ", "_") ?? "report"}_{DateTime.UtcNow:yyyyMMdd}.csv");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error generating CSV report: {ex.Message}");
            }
        }

        // CSV Helper Methods
        private void AddStudentDataToCsv(StringBuilder csvBuilder, StudentReportDTO? data)
        {
            if (data?.Students == null || !data.Students.Any())
            {
                csvBuilder.AppendLine("No student data available");
                return;
            }

            // Headers
            csvBuilder.AppendLine("StudentID,Name,Email,TotalBookings,CompletedSessions,CancelledSessions,EngagementScore,Status");

            // Data rows
            foreach (var student in data.Students)
            {
                csvBuilder.AppendLine($"{student.StudentId},{EscapeCsv(student.Name)},{EscapeCsv(student.Email)},{student.TotalBookings},{student.CompletedSessions},{student.CancelledSessions},{student.EngagementScore:F2},{(student.IsActive ? "Active" : "Inactive")}");
            }

            // Summary
            csvBuilder.AppendLine();
            csvBuilder.AppendLine("Summary");
            csvBuilder.AppendLine($"Total Registrations,{data.TotalRegistrations}");
            csvBuilder.AppendLine($"Total Bookings,{data.TotalBookings}");
            csvBuilder.AppendLine($"Average Sessions per Student,{Math.Round(data.AverageSessionsPerStudent, 2)}");
        }

        private void AddTutorDataToCsv(StringBuilder csvBuilder, TutorReportDTO? data)
        {
            if (data?.Tutors == null || !data.Tutors.Any())
            {
                csvBuilder.AppendLine("No tutor data available");
                return;
            }

            csvBuilder.AppendLine("TutorID,Name,Email,Modules,AverageRating,TotalSessions,CompletedSessions,Status");

            foreach (var tutor in data.Tutors)
            {
                csvBuilder.AppendLine($"{tutor.TutorId},{EscapeCsv(tutor.Name)},{EscapeCsv(tutor.Email)},{EscapeCsv(string.Join("; ", tutor.Modules ?? new List<string>()))},{Math.Round(tutor.AverageRating, 2)},{tutor.TotalSessions},{tutor.CompletedSessions},{(tutor.IsActive ? "Active" : "Inactive")}");
            }

            csvBuilder.AppendLine();
            csvBuilder.AppendLine("Summary");
            csvBuilder.AppendLine($"Active Tutors,{data.ActiveTutors}");
            csvBuilder.AppendLine($"Average Rating,{Math.Round(data.AverageRating, 2)}");
            csvBuilder.AppendLine($"Total Completed Sessions,{data.TotalCompletedSessions}");
        }

        private void AddBookingDataToCsv(StringBuilder csvBuilder, BookingReportDTO? data)
        {
            if (data?.Bookings == null || !data.Bookings.Any())
            {
                csvBuilder.AppendLine("No booking data available");
                return;
            }

            csvBuilder.AppendLine("BookingID,StudentName,TutorName,Module,SessionDate,Duration,Status");

            foreach (var booking in data.Bookings)
            {
                csvBuilder.AppendLine($"{booking.BookingId},{EscapeCsv(booking.StudentName)},{EscapeCsv(booking.TutorName)},{EscapeCsv(booking.Module)},{booking.SessionDate:yyyy-MM-dd HH:mm},{booking.Duration},{EscapeCsv(booking.Status)}");
            }

            csvBuilder.AppendLine();
            csvBuilder.AppendLine("Summary");
            csvBuilder.AppendLine($"Pending,{data.PendingCount}");
            csvBuilder.AppendLine($"Confirmed,{data.ConfirmedCount}");
            csvBuilder.AppendLine($"Completed,{data.CompletedCount}");
            csvBuilder.AppendLine($"Cancelled,{data.CancelledCount}");
        }

        private void AddModuleDemandDataToCsv(StringBuilder csvBuilder, ModuleDemandReportDTO? data)
        {
            if (data?.Modules == null || !data.Modules.Any())
            {
                csvBuilder.AppendLine("No module data available");
                return;
            }

            csvBuilder.AppendLine("ModuleCode,ModuleName,TotalBookings,UniqueTutors,UniqueStudents,GrowthRate");

            foreach (var module in data.Modules)
            {
                csvBuilder.AppendLine($"{EscapeCsv(module.ModuleCode)},{EscapeCsv(module.ModuleName)},{module.TotalBookings},{module.UniqueTutors},{module.UniqueStudents},{Math.Round(module.GrowthRate, 2)}");
            }

            csvBuilder.AppendLine();
            csvBuilder.AppendLine("Summary");
            csvBuilder.AppendLine($"Total Bookings,{data.TotalBookings}");
            csvBuilder.AppendLine($"Most Popular Module,{EscapeCsv(data.MostPopularModule)}");
            csvBuilder.AppendLine($"Least Popular Module,{EscapeCsv(data.LeastPopularModule)}");
        }

        private void AddEverythingDataToCsv(StringBuilder csvBuilder, EverythingReportDTO? data)
        {
            if (data == null)
            {
                csvBuilder.AppendLine("No data available for comprehensive report");
                return;
            }

            var overallStats = data.OverallStats;

            // Overall Summary Section
            csvBuilder.AppendLine("OVERALL SYSTEM SUMMARY");
            csvBuilder.AppendLine("Metric,Value");
            csvBuilder.AppendLine($"Total Students,{overallStats.TotalStudents}");
            csvBuilder.AppendLine($"Total Tutors,{overallStats.TotalTutors}");
            csvBuilder.AppendLine($"Total Bookings,{overallStats.TotalBookings}");
            csvBuilder.AppendLine($"Total Modules,{overallStats.TotalModules}");
            csvBuilder.AppendLine($"Active Users,{overallStats.ActiveUsers}");
            csvBuilder.AppendLine($"Average Tutor Rating,{overallStats.AverageRating:F2}");
            csvBuilder.AppendLine();

            // Student Data Section
            if (data.StudentReport?.Students != null && data.StudentReport.Students.Any())
            {
                csvBuilder.AppendLine("STUDENT DATA SUMMARY");
                csvBuilder.AppendLine($"Total Students,{data.StudentReport.TotalRegistrations}");
                csvBuilder.AppendLine($"Total Bookings by Students,{data.StudentReport.TotalBookings}");
                csvBuilder.AppendLine($"Average Sessions per Student,{Math.Round(data.StudentReport.AverageSessionsPerStudent, 2)}");
                csvBuilder.AppendLine();
            }

            // Tutor Data Section
            if (data.TutorReport?.Tutors != null && data.TutorReport.Tutors.Any())
            {
                csvBuilder.AppendLine("TUTOR DATA SUMMARY");
                csvBuilder.AppendLine($"Total Tutors,{data.TutorReport.Tutors.Count}");
                csvBuilder.AppendLine($"Active Tutors,{data.TutorReport.ActiveTutors}");
                csvBuilder.AppendLine($"Average Rating,{Math.Round(data.TutorReport.AverageRating, 2)}");
                csvBuilder.AppendLine($"Total Completed Sessions,{data.TutorReport.TotalCompletedSessions}");
                csvBuilder.AppendLine();
            }

            // Booking Data Section
            if (data.BookingReport?.Bookings != null && data.BookingReport.Bookings.Any())
            {
                csvBuilder.AppendLine("BOOKING DATA SUMMARY");
                csvBuilder.AppendLine($"Pending Bookings,{data.BookingReport.PendingCount}");
                csvBuilder.AppendLine($"Confirmed Bookings,{data.BookingReport.ConfirmedCount}");
                csvBuilder.AppendLine($"Completed Bookings,{data.BookingReport.CompletedCount}");
                csvBuilder.AppendLine($"Cancelled Bookings,{data.BookingReport.CancelledCount}");
                csvBuilder.AppendLine();
            }

            // Module Demand Section
            if (data.ModuleDemandReport?.Modules != null && data.ModuleDemandReport.Modules.Any())
            {
                csvBuilder.AppendLine("MODULE DEMAND SUMMARY");
                csvBuilder.AppendLine($"Total Module Bookings,{data.ModuleDemandReport.TotalBookings}");
                csvBuilder.AppendLine($"Most Popular Module,{EscapeCsv(data.ModuleDemandReport.MostPopularModule)}");
                csvBuilder.AppendLine($"Least Popular Module,{EscapeCsv(data.ModuleDemandReport.LeastPopularModule)}");
                csvBuilder.AppendLine();

                // Top Modules
                csvBuilder.AppendLine("TOP 5 MODULES BY DEMAND");
                csvBuilder.AppendLine("Module Name,Total Bookings,Unique Tutors,Unique Students");

                var topModules = data.ModuleDemandReport.Modules.Take(5);
                foreach (var module in topModules)
                {
                    csvBuilder.AppendLine($"{EscapeCsv(module.ModuleName)},{module.TotalBookings},{module.UniqueTutors},{module.UniqueStudents}");
                }
            }
        }

        private string EscapeCsv(string? value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }
            return value;
        }
    }

}