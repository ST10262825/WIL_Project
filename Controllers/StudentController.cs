using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TutorConnectAPI.Data;
using TutorConnectAPI.DTOs;
using TutorConnectAPI.Models;

namespace TutorConnectAPI.Controllers
{
    [ApiController]
    [Route("api/student")]
    public class StudentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public StudentController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: api/student/by-user/{userId}
        [HttpGet("by-user/{userId}")]
        public async Task<IActionResult> GetStudentByUserId(int userId)
        {
            try
            {
                var student = await _context.Students
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.UserId == userId);

                if (student == null)
                    return NotFound("Student not found");

                var studentDto = new StudentDTO
                {
                    StudentId = student.StudentId,
                    UserId = student.UserId,
                    Name = student.Name,
                    Bio = student.Bio,
                    ProfileImage = student.ProfileImage,
                   // CreatedDate = student.CreatedDate
                };

                return Ok(studentDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while fetching student data");
            }
        }

        // GET: api/student/{studentId}/bookings
        [HttpGet("{studentId}/bookings")]
        public async Task<IActionResult> GetStudentBookings(int studentId)
        {
            try
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
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while fetching bookings");
            }
        }

        // GET: api/student/{studentId}/dashboard-summary
        [HttpGet("{studentId}/dashboard-summary")]
        public async Task<IActionResult> GetDashboardSummary(int studentId)
        {
            try
            {
                // Check if student exists
                var studentExists = await _context.Students.AnyAsync(s => s.StudentId == studentId);
                if (!studentExists)
                    return NotFound("Student not found");

                // Get all student bookings
                var bookings = await _context.Bookings
                    .Where(b => b.StudentId == studentId)
                    .ToListAsync();

                // Calculate statistics
                var completedSessions = bookings.Count(b => b.Status == "Completed");
                var totalHours = bookings.Where(b => b.Status == "Completed")
                                       .Sum(b => (b.EndTime - b.StartTime).TotalHours);

                var activeTutors = bookings.Where(b => b.Status == "Accepted" || b.Status == "Pending")
                                         .Select(b => b.TutorId)
                                         .Distinct()
                                         .Count();

                var summary = new StudentDashboardSummaryDTO
                {
                    StudentId = studentId,
                    UpcomingSessionsCount = bookings.Count(b => b.StartTime >= DateTime.Today &&
                                                              (b.Status == "Accepted" || b.Status == "Pending")),
                    TotalLearningHours = (int)totalHours,
                    CompletedSessionsCount = completedSessions,
                    ActiveTutorsCount = activeTutors,
                    PendingBookingsCount = bookings.Count(b => b.Status == "Pending")
                };

                return Ok(summary);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while fetching dashboard summary");
            }
        }

        // PUT: api/student/{studentId}/profile
        [HttpPut("{studentId}/profile")]
        public async Task<IActionResult> UpdateStudentProfile(int studentId, [FromForm] UpdateStudentProfileDTO dto)
        {
            try
            {
                var student = await _context.Students.FindAsync(studentId);
                if (student == null)
                    return NotFound(new { message = "Student not found" });

                // Update bio
                if (!string.IsNullOrEmpty(dto.Bio))
                {
                    student.Bio = dto.Bio;
                }

                string newImageUrl = student.ProfileImage;

                // Handle profile image upload
                if (dto.ProfileImage != null && dto.ProfileImage.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "images");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    // Delete old profile image if exists
                    if (!string.IsNullOrEmpty(student.ProfileImage))
                    {
                        var oldFilePath = Path.Combine(_environment.WebRootPath, student.ProfileImage.TrimStart('/').Replace("~/", ""));
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    // Generate unique filename
                    var fileName = $"student_{studentId}_{Guid.NewGuid()}{Path.GetExtension(dto.ProfileImage.FileName)}";
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await dto.ProfileImage.CopyToAsync(stream);
                    }

                    // FIX: Use ~/ format for consistency with Url.Content()
                    newImageUrl = $"~/images/{fileName}";
                    student.ProfileImage = newImageUrl;
                }

                _context.Students.Update(student);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Profile updated successfully",
                    profileImageUrl = newImageUrl,  // This will now be "~/images/filename.jpg"
                    bio = student.Bio
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while updating profile" });
            }
        }


        // GET: api/student/{studentId}/profile-image
        [HttpGet("{studentId}/profile-image")]
        public IActionResult GetProfileImage(int studentId)
        {
            try
            {
                var student = _context.Students.Find(studentId);
                if (student == null || string.IsNullOrEmpty(student.ProfileImage))
                {
                    // Return default image
                    var defaultImagePath = Path.Combine(_environment.WebRootPath, "images", "default-profile.png");
                    return PhysicalFile(defaultImagePath, "image/png");
                }

                var fileName = student.ProfileImage.Replace("~/images/", "").Replace("/images/", "");
                var filePath = Path.Combine(_environment.WebRootPath, "images", fileName);

                if (System.IO.File.Exists(filePath))
                {
                    var contentType = "image/jpeg";
                    if (fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                        contentType = "image/png";
                    else if (fileName.EndsWith(".gif", StringComparison.OrdinalIgnoreCase))
                        contentType = "image/gif";

                    return PhysicalFile(filePath, contentType);
                }
                else
                {
                    var defaultImagePath = Path.Combine(_environment.WebRootPath, "images", "default-profile.png");
                    return PhysicalFile(defaultImagePath, "image/png");
                }
            }
            catch (Exception ex)
            {
                var defaultImagePath = Path.Combine(_environment.WebRootPath, "images", "default-profile.png");
                return PhysicalFile(defaultImagePath, "image/png");
            }
        }


        // GET: api/student/{studentId}/progress
        [HttpGet("{studentId}/progress")]
        public async Task<IActionResult> GetStudentProgress(int studentId)
        {
            try
            {
                var progress = await _context.Bookings
                    .Include(b => b.Module)
                    .Where(b => b.StudentId == studentId)
                    .GroupBy(b => b.Module.Name)
                    .Select(g => new StudentProgressDTO
                    {
                        ModuleName = g.Key,
                        TotalSessions = g.Count(),
                        CompletedSessions = g.Count(b => b.Status == "Completed"),
                        PercentageComplete = g.Count(b => b.Status == "Completed") * 100 / Math.Max(1, g.Count())
                    })
                    .ToListAsync();

                return Ok(progress);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while fetching progress data");
            }
        }

        // GET: api/student/{studentId}/upcoming-sessions
        [HttpGet("{studentId}/upcoming-sessions")]
        public async Task<IActionResult> GetUpcomingSessions(int studentId)
        {
            try
            {
                var sessions = await _context.Bookings
                    .Include(b => b.Tutor).ThenInclude(t => t.User)
                    .Include(b => b.Module)
                    .Where(b => b.StudentId == studentId &&
                               b.StartTime >= DateTime.Today &&
                               (b.Status == "Accepted" || b.Status == "Pending"))
                    .OrderBy(b => b.StartTime)
                    .Select(b => new BookingDTO
                    {
                        BookingId = b.BookingId,
                        TutorId = b.TutorId,
                        TutorName = b.Tutor.Name + " " + b.Tutor.Surname,
                        ModuleName = b.Module.Name,
                        StartTime = b.StartTime,
                        EndTime = b.EndTime,
                        Status = b.Status,
                        Notes = b.Notes
                    })
                    .ToListAsync();

                return Ok(sessions);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while fetching upcoming sessions");
            }
        }
    }
}