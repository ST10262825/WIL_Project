//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using TutorConnectAPI.Data;
//using TutorConnectAPI.DTOs;
//using TutorConnectAPI.Models;

//namespace TutorConnectAPI.Controllers
//{
//    [ApiController]
//    [Route("api/admin")]
//    [Authorize(Roles = "Admin")]
//    public class AdminDashboardController : ControllerBase
//    {
//        private readonly ApplicationDbContext _context;
//        private readonly ILogger<AdminDashboardController> _logger;

//        public AdminDashboardController(ApplicationDbContext context, ILogger<AdminDashboardController> logger)
//        {
//            _context = context;
//            _logger = logger;
//        }

//        #region Dashboard Statistics
//        [HttpGet("dashboard-stats")]
//        public async Task<IActionResult> GetDashboardStats()
//        {
//            try
//            {
//                var stats = new
//                {
//                    TotalTutors = await _context.Tutors.CountAsync(),
//                    TotalStudents = await _context.Students.CountAsync(),
//                    TotalBookings = await _context.Bookings.CountAsync(),
//                    PendingBookings = await _context.Bookings.CountAsync(b => b.Status == "Pending"),
//                    ActiveModules = await _context.Modules.CountAsync(),
//                    TotalRevenue = await _context.Bookings
//                        .Where(b => b.Status == "Completed")
//                        .SumAsync(b => (b.EndTime - b.StartTime).TotalHours * 50) // Assuming $50/hour
//                };

//                return Ok(stats);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error fetching dashboard statistics");
//                return StatusCode(500, "Error loading dashboard statistics");
//            }
//        }
//        #endregion

//        #region Tutor Management
//        [HttpGet("tutors")]
//        public async Task<IActionResult> GetTutors([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
//        {
//            try
//            {
//                var tutors = await _context.Tutors
//                    .Include(t => t.User)
//                    .Include(t => t.TutorModules)
//                        .ThenInclude(tm => tm.Module)
//                    .OrderBy(t => t.Name)
//                    .Skip((page - 1) * pageSize)
//                    .Take(pageSize)
//                    .ToListAsync();

//                var totalCount = await _context.Tutors.CountAsync();

//                var tutorDtos = tutors.Select(t => new TutorDTO
//                {
//                    TutorId = t.TutorId,
//                    Name = t.Name,
//                    Surname = t.Surname,
//                    Phone = t.Phone,
//                    Bio = t.Bio,
//                    IsBlocked = t.IsBlocked,
//                    ProfileImageUrl = t.ProfileImageUrl,
//                    AboutMe = t.AboutMe,
//                    Expertise = t.Expertise,
//                    Education = t.Education,
//                    Modules = t.TutorModules.Select(tm => new ModuleDTO
//                    {
//                        Id = tm.Module.ModuleId,
//                        Code = tm.Module.Code,
//                        Name = tm.Module.Name
//                    }).ToList()
//                }).ToList();

//                return Ok(new { Tutors = tutorDtos, TotalCount = totalCount });
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error fetching tutors");
//                return StatusCode(500, "Error loading tutors");
//            }
//        }

//        [HttpDelete("tutors/{id}")]
//        public async Task<IActionResult> DeleteTutor(int id)
//        {
//            try
//            {
//                var tutor = await _context.Tutors
//                    .Include(t => t.Bookings)
//                    .Include(t => t.TutorModules)
//                    .FirstOrDefaultAsync(t => t.TutorId == id);

//                if (tutor == null)
//                    return NotFound("Tutor not found.");

//                // Check for active bookings
//                var activeBookings = tutor.Bookings.Any(b =>
//                    b.Status != "Completed" && b.Status != "Cancelled" && b.Status != "Declined");

//                if (activeBookings)
//                    return BadRequest("Cannot delete tutor with active bookings. Please cancel all bookings first.");

//                // Soft delete approach - mark as blocked instead of physical delete
//                tutor.IsBlocked = true;
//                await _context.SaveChangesAsync();

//                return Ok("Tutor blocked successfully.");
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error deleting tutor with ID: {TutorId}", id);
//                return StatusCode(500, "Error deleting tutor");
//            }
//        }

//        [HttpPut("tutors/{id}/toggle-block")]
//        public async Task<IActionResult> ToggleBlockTutor(int id)
//        {
//            try
//            {
//                var tutor = await _context.Tutors.FindAsync(id);
//                if (tutor == null)
//                    return NotFound("Tutor not found.");

//                tutor.IsBlocked = !tutor.IsBlocked;
//                await _context.SaveChangesAsync();

//                return Ok(new
//                {
//                    Message = tutor.IsBlocked ? "Tutor blocked successfully." : "Tutor unblocked successfully.",
//                    IsBlocked = tutor.IsBlocked
//                });
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error toggling block status for tutor ID: {TutorId}", id);
//                return StatusCode(500, "Error updating tutor status");
//            }
//        }
//        #endregion

//        #region Student Management
//        [HttpGet("students")]
//        public async Task<IActionResult> GetStudents([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
//        {
//            try
//            {
//                var students = await _context.Students
//                    .Include(s => s.User)
//                    .Include(s => s.Bookings)
//                    .OrderBy(s => s.Name)
//                    .Skip((page - 1) * pageSize)
//                    .Take(pageSize)
//                    .ToListAsync();

//                var totalCount = await _context.Students.CountAsync();

//                var studentDtos = students.Select(s => new StudentDTO
//                {
//                    StudentId = s.StudentId,
//                    UserId = s.UserId,
//                    Name = s.Name,
//                    Course = s.Course,
//                    Bio = s.Bio,
//                    ProfileImage = s.ProfileImage,
//                  //  TotalBookings = s.Bookings.Count,
//                   // CompletedBookings = s.Bookings.Count(b => b.Status == "Completed")
//                }).ToList();

//                return Ok(new { Students = studentDtos, TotalCount = totalCount });
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error fetching students");
//                return StatusCode(500, "Error loading students");
//            }
//        }

//        [HttpPut("students/{id}/toggle-block")]
//        public async Task<IActionResult> ToggleBlockStudent(int id)
//        {
//            try
//            {
//                var student = await _context.Students
//                    .Include(s => s.User)
//                    .FirstOrDefaultAsync(s => s.StudentId == id);

//                if (student == null)
//                    return NotFound("Student not found.");

//                student.User.IsActive = !student.User.IsActive;
//                await _context.SaveChangesAsync();

//                return Ok(new
//                {
//                    Message = student.User.IsActive ? "Student activated successfully." : "Student blocked successfully.",
//                    IsBlocked = !student.User.IsActive
//                });
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error toggling block status for student ID: {StudentId}", id);
//                return StatusCode(500, "Error updating student status");
//            }
//        }
//        #endregion

//        #region Booking Management
//        [HttpGet("bookings")]
//        public async Task<IActionResult> GetBookings([FromQuery] int page = 1, [FromQuery] int pageSize = 10,
//                                                   [FromQuery] string status = "all")
//        {
//            try
//            {
//                var query = _context.Bookings
//                    .Include(b => b.Tutor)
//                    .Include(b => b.Student)
//                    .Include(b => b.Module)
//                    .AsQueryable();

//                if (status != "all")
//                    query = query.Where(b => b.Status == status);

//                var bookings = await query
//                    .OrderByDescending(b => b.StartTime)
//                    .Skip((page - 1) * pageSize)
//                    .Take(pageSize)
//                    .ToListAsync();

//                var totalCount = await query.CountAsync();

//                var bookingDtos = bookings.Select(b => new BookingDTO
//                {
//                    BookingId = b.BookingId,
//                    TutorId = b.TutorId,
//                    TutorName = $"{b.Tutor.Name} {b.Tutor.Surname}",
//                    StudentId = b.StudentId,
//                    StudentName = b.Student.Name,
//                    ModuleName = b.Module.Name,
//                    StartTime = b.StartTime,
//                    EndTime = b.EndTime,
//                    Status = b.Status,
//                    Notes = b.Notes
//                }).ToList();

//                return Ok(new { Bookings = bookingDtos, TotalCount = totalCount });
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error fetching bookings");
//                return StatusCode(500, "Error loading bookings");
//            }
//        }

//        [HttpPut("bookings/{id}/status")]
//        public async Task<IActionResult> UpdateBookingStatus(int id, [FromBody] UpdateBookingStatusRequest request)
//        {
//            try
//            {
//                var booking = await _context.Bookings.FindAsync(id);
//                if (booking == null)
//                    return NotFound("Booking not found.");

//                // Validate status transition
//                var validTransitions = new Dictionary<string, string[]>
//                {
//                    ["Pending"] = new[] { "Accepted", "Declined", "Cancelled" },
//                    ["Accepted"] = new[] { "Completed", "Cancelled" },
//                    ["Declined"] = new[] { "Cancelled" },
//                    ["Cancelled"] = new string[] { }
//                };

//                if (!validTransitions.ContainsKey(booking.Status) ||
//                    !validTransitions[booking.Status].Contains(request.Status))
//                {
//                    return BadRequest($"Invalid status transition from {booking.Status} to {request.Status}");
//                }

//                booking.Status = request.Status;
//                await _context.SaveChangesAsync();

//                return Ok("Booking status updated successfully.");
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error updating booking status for ID: {BookingId}", id);
//                return StatusCode(500, "Error updating booking status");
//            }
//        }

//        [HttpDelete("bookings/{id}")]
//        public async Task<IActionResult> DeleteBooking(int id)
//        {
//            try
//            {
//                var booking = await _context.Bookings.FindAsync(id);
//                if (booking == null)
//                    return NotFound("Booking not found.");

//                // Only allow deletion of certain statuses
//                if (booking.Status == "Completed" || booking.Status == "Accepted")
//                    return BadRequest("Cannot delete completed or accepted bookings.");

//                _context.Bookings.Remove(booking);
//                await _context.SaveChangesAsync();

//                return Ok("Booking deleted successfully.");
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error deleting booking ID: {BookingId}", id);
//                return StatusCode(500, "Error deleting booking");
//            }
//        }
//        #endregion

//        #region Module Management
//        [HttpGet("modules")]
//        public async Task<IActionResult> GetModules()
//        {
//            try
//            {
//                var modules = await _context.Modules
//                    .Include(m => m.TutorModules)
//                    .OrderBy(m => m.Name)
//                    .ToListAsync();

//                var moduleDtos = modules.Select(m => new ModuleDTO
//                {
//                    Id = m.ModuleId,
//                    Code = m.Code,
//                    Name = m.Name,
//                    TutorCount = m.TutorModules.Count
//                }).ToList();

//                return Ok(moduleDtos);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error fetching modules");
//                return StatusCode(500, "Error loading modules");
//            }
//        }

//        [HttpPost("modules")]
//        public async Task<IActionResult> CreateModule([FromBody] CreateModuleRequest request)
//        {
//            try
//            {
//                if (string.IsNullOrEmpty(request.Code) || string.IsNullOrEmpty(request.Name))
//                    return BadRequest("Module code and name are required.");

//                // Check for duplicate module code
//                var existingModule = await _context.Modules
//                    .FirstOrDefaultAsync(m => m.Code == request.Code);

//                if (existingModule != null)
//                    return BadRequest("Module with this code already exists.");

//                var module = new Module
//                {
//                    Code = request.Code,
//                    Name = request.Name
//                };

//                _context.Modules.Add(module);
//                await _context.SaveChangesAsync();

//                return Ok(new { Message = "Module created successfully.", ModuleId = module.ModuleId });
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error creating module");
//                return StatusCode(500, "Error creating module");
//            }
//        }

//        [HttpPut("modules/{id}")]
//        public async Task<IActionResult> UpdateModule(int id, [FromBody] UpdateModuleRequest request)
//        {
//            try
//            {
//                var module = await _context.Modules.FindAsync(id);
//                if (module == null)
//                    return NotFound("Module not found.");

//                // Check for duplicate code (excluding current module)
//                var duplicateModule = await _context.Modules
//                    .FirstOrDefaultAsync(m => m.Code == request.Code && m.ModuleId != id);

//                if (duplicateModule != null)
//                    return BadRequest("Another module with this code already exists.");

//                module.Code = request.Code;
//                module.Name = request.Name;

//                await _context.SaveChangesAsync();

//                return Ok("Module updated successfully.");
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error updating module ID: {ModuleId}", id);
//                return StatusCode(500, "Error updating module");
//            }
//        }

//        [HttpDelete("modules/{id}")]
//        public async Task<IActionResult> DeleteModule(int id)
//        {
//            try
//            {
//                var module = await _context.Modules
//                    .Include(m => m.TutorModules)
//                    .Include(m => m.Bookings)
//                    .FirstOrDefaultAsync(m => m.ModuleId == id);

//                if (module == null)
//                    return NotFound("Module not found.");

//                // Check if module is in use
//                if (module.TutorModules.Any() || module.Bookings.Any())
//                    return BadRequest("Cannot delete module that is assigned to tutors or has bookings.");

//                _context.Modules.Remove(module);
//                await _context.SaveChangesAsync();

//                return Ok("Module deleted successfully.");
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error deleting module ID: {ModuleId}", id);
//                return StatusCode(500, "Error deleting module");
//            }
//        }
//        #endregion

//        #region System Health & Logs
//        [HttpGet("system-health")]
//        public async Task<IActionResult> GetSystemHealth()
//        {
//            try
//            {
//                var healthInfo = new
//                {
//                    DatabaseStatus = "Healthy", // You can add actual database health check
//                    Uptime = Environment.TickCount / 1000, // seconds
//                    MemoryUsage = GC.GetTotalMemory(false) / 1024 / 1024, // MB
//                    ActiveConnections = await _context.Bookings.CountAsync(b =>
//                        b.Status == "Accepted" && b.StartTime <= DateTime.Now && b.EndTime >= DateTime.Now)
//                };

//                return Ok(healthInfo);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error checking system health");
//                return StatusCode(500, "Error checking system health");
//            }
//        }
//        #endregion
//    }

//    #region Request DTOs
//    public class UpdateBookingStatusRequest
//    {
//        public string Status { get; set; }
//        public string Reason { get; set; }
//    }

//    public class CreateModuleRequest
//    {
//        public string Code { get; set; }
//        public string Name { get; set; }
//    }

//    public class UpdateModuleRequest
//    {
//        public string Code { get; set; }
//        public string Name { get; set; }
//    }
//    #endregion
//}