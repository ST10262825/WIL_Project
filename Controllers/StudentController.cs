using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
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
        private readonly ILogger<StudentController> _logger;

        public StudentController(ApplicationDbContext context, IWebHostEnvironment environment, ILogger<StudentController> logger)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
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




        // GET: api/student/{studentId}/materials
        [HttpGet("{studentId}/materials")]
        public async Task<IActionResult> GetStudentMaterials(int studentId)
        {
            try
            {
                Console.WriteLine($"[API] GetStudentMaterials called for student {studentId}");

                // Get accepted and completed bookings within access period
                var accessibleBookings = await _context.Bookings
                    .Where(b => b.StudentId == studentId &&
                               (b.Status == "Accepted" ||
                                (b.Status == "Completed" &&
                                 b.CompletedAt.HasValue &&
                                 b.CompletedAt.Value.AddDays(2) >= DateTime.UtcNow)))
                    .Select(b => b.TutorId)
                    .Distinct()
                    .ToListAsync();

                Console.WriteLine($"[API] Accessible bookings found: {accessibleBookings.Count} - Tutors: {string.Join(", ", accessibleBookings)}");

                // Get materials: public OR from tutors the student has accessible sessions with
                var accessibleMaterials = await _context.LearningMaterials
                    .Include(m => m.Tutor)
                    .Include(m => m.Folder)
                    .Where(m => m.IsPublic || accessibleBookings.Contains(m.TutorId))
                    .ToListAsync();

                Console.WriteLine($"[API] Accessible materials found: {accessibleMaterials.Count}");

                var result = accessibleMaterials.Select(m => new LearningMaterialDTO
                {
                    LearningMaterialId = m.LearningMaterialId,
                    TutorId = m.TutorId,
                    TutorName = m.Tutor != null ? m.Tutor.Name + " " + m.Tutor.Surname : "Unknown Tutor",
                    FolderId = m.FolderId,
                    FolderName = m.Folder != null ? m.Folder.Name : "Root",
                    Title = m.Title,
                    Description = m.Description,
                    FileName = m.FileName,
                    FileUrl = $"{Request.Scheme}://{Request.Host}/api/learning-materials/materials/{m.LearningMaterialId}/download?studentId={studentId}",
                    FileType = m.FileType,
                    FileSize = m.FileSize,
                    FileSizeFormatted = FormatFileSize(m.FileSize),
                    IsPublic = m.IsPublic,
                    CreatedAt = m.CreatedAt,
                    UpdatedAt = m.UpdatedAt
                })
                .OrderByDescending(m => m.CreatedAt)
                .ToList();

                Console.WriteLine($"[API] Returning {result.Count} materials");
                return Ok(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] ERROR in GetStudentMaterials: {ex.Message}");
                Console.WriteLine($"[API] Stack trace: {ex.StackTrace}");
                _logger.LogError(ex, "Error getting student materials for student {StudentId}", studentId);
                return StatusCode(500, "Error retrieving materials");
            }
        }

        // GET: api/student/{studentId}/materials/tutor/{tutorId}
        [HttpGet("{studentId}/materials/tutor/{tutorId}")]
        public async Task<IActionResult> GetTutorMaterialsForStudent(int studentId, int tutorId)
        {
            try
            {
                // Check if student has accessible sessions with this tutor
                var hasAccess = await _context.Bookings
                    .AnyAsync(b => b.StudentId == studentId &&
                                  b.TutorId == tutorId &&
                                  (b.Status == "Accepted" ||
                                   (b.Status == "Completed" &&
                                    b.CompletedAt.HasValue &&
                                    b.CompletedAt.Value.AddDays(2) >= DateTime.UtcNow)));

                if (!hasAccess)
                {
                    return StatusCode(403, "You don't have access to this tutor's materials. Book a session or your access period may have expired.");
                }

                var materials = await _context.LearningMaterials
                    .Include(m => m.Tutor)
                    .Include(m => m.Folder)
                    .Where(m => m.TutorId == tutorId && (m.IsPublic || hasAccess))
                    .Select(m => new LearningMaterialDTO
                    {
                        LearningMaterialId = m.LearningMaterialId,
                        TutorId = m.TutorId,
                        TutorName = m.Tutor.Name + " " + m.Tutor.Surname,
                        FolderId = m.FolderId,
                        FolderName = m.Folder != null ? m.Folder.Name : "Root",
                        Title = m.Title,
                        Description = m.Description,
                        FileName = m.FileName,
                        FileUrl = $"{Request.Scheme}://{Request.Host}/api/learning-materials/materials/{m.LearningMaterialId}/download?studentId={studentId}",
                        FileType = m.FileType,
                        FileSize = m.FileSize,
                        FileSizeFormatted = FormatFileSize(m.FileSize),
                        IsPublic = m.IsPublic,
                        CreatedAt = m.CreatedAt,
                        UpdatedAt = m.UpdatedAt
                    })
                    .OrderByDescending(m => m.CreatedAt)
                    .ToListAsync();

                return Ok(materials);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tutor materials for student {StudentId}", studentId);
                return StatusCode(500, "Error retrieving materials");
            }
        }

        // GET: api/student/{studentId}/materials/overview
        [HttpGet("{studentId}/materials/overview")]
        public async Task<IActionResult> GetStudentMaterialsOverview(int studentId)
        {
            try
            {
                // Get accessible bookings
                var accessibleBookings = await _context.Bookings
                    .Where(b => b.StudentId == studentId &&
                               (b.Status == "Accepted" ||
                                (b.Status == "Completed" &&
                                 b.CompletedAt.HasValue &&
                                 b.CompletedAt.Value.AddDays(2) >= DateTime.UtcNow)))
                    .Select(b => b.TutorId)
                    .Distinct()
                    .ToListAsync();

                var totalMaterials = await _context.LearningMaterials
                    .CountAsync(m => m.IsPublic || accessibleBookings.Contains(m.TutorId));

                // Get tutors with materials
                var tutorsWithMaterials = await _context.LearningMaterials
                    .Include(m => m.Tutor)
                    .Where(m => m.IsPublic || accessibleBookings.Contains(m.TutorId))
                    .Select(m => new { m.TutorId, m.Tutor.Name, m.Tutor.Surname })
                    .Distinct()
                    .ToListAsync();

                var tutorDTOs = tutorsWithMaterials.Select(t => new
                {
                    TutorId = t.TutorId,
                    TutorName = t.Name + " " + t.Surname
                }).ToList();

                // Get recent materials
                var recentMaterials = await _context.LearningMaterials
                    .Include(m => m.Tutor)
                    .Include(m => m.Folder)
                    .Where(m => m.IsPublic || accessibleBookings.Contains(m.TutorId))
                    .OrderByDescending(m => m.CreatedAt)
                    .Take(5)
                    .ToListAsync();

                var recentMaterialDTOs = recentMaterials.Select(m => new LearningMaterialDTO
                {
                    LearningMaterialId = m.LearningMaterialId,
                    TutorId = m.TutorId,
                    TutorName = m.Tutor != null ? m.Tutor.Name + " " + m.Tutor.Surname : "Unknown Tutor",
                    FolderId = m.FolderId,
                    FolderName = m.Folder != null ? m.Folder.Name : "Root",
                    Title = m.Title,
                    Description = m.Description,
                    FileName = m.FileName,
                    FileType = m.FileType,
                    FileSize = m.FileSize,
                    FileSizeFormatted = FormatFileSize(m.FileSize),
                    IsPublic = m.IsPublic,
                    CreatedAt = m.CreatedAt
                }).ToList();

                var overview = new
                {
                    TotalMaterials = totalMaterials,
                    TotalTutors = tutorDTOs.Count,
                    Tutors = tutorDTOs,
                    RecentMaterials = recentMaterialDTOs,
                    AccessInfo = "Materials accessible for accepted bookings and 2 days after completion"
                };

                return Ok(overview);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting materials overview for student {StudentId}", studentId);
                return StatusCode(500, "Error retrieving materials overview");
            }
        }



        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            double len = bytes;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        // Add to your API StudentController
        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDTO dto)
        {
            try
            {
                Console.WriteLine($"[API] ChangePassword called for user");

                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                Console.WriteLine($"[API] User ID: {userId}");

                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                {
                    Console.WriteLine($"[API] User not found");
                    return BadRequest("User not found.");
                }

                Console.WriteLine($"[API] User found: {user.Email}");

                // Verify current password
                bool isCurrentPasswordValid = BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash);
                Console.WriteLine($"[API] Current password valid: {isCurrentPasswordValid}");

                if (!isCurrentPasswordValid)
                {
                    return BadRequest("Current password is incorrect.");
                }

                // Hash new password
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
                await _context.SaveChangesAsync();

                Console.WriteLine($"[API] Password changed successfully");
                return Ok("Password changed successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] Error changing password: {ex.Message}");
                Console.WriteLine($"[API] Stack trace: {ex.StackTrace}");
                return StatusCode(500, "Error changing password.");
            }
        }



        [HttpPut("toggle-theme")]
        public async Task<IActionResult> ToggleTheme()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var user = await _context.Users.FindAsync(userId);

                if (user == null) return NotFound("User not found.");

                user.ThemePreference = user.ThemePreference == "light" ? "dark" : "light";
                await _context.SaveChangesAsync();

                return Ok(new { theme = user.ThemePreference });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error updating theme.");
            }
        }

        [HttpDelete("delete-account/{studentId}")]
        public async Task<IActionResult> DeleteAccount(int studentId)
        {
            try
            {
                var student = await _context.Students
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.StudentId == studentId);

                if (student == null) return NotFound("Student not found.");

                // Check for active bookings
                var hasActiveBookings = await _context.Bookings
                    .AnyAsync(b => b.StudentId == studentId &&
                                  (b.Status == "Pending" || b.Status == "Accepted"));

                if (hasActiveBookings)
                    return BadRequest("Cannot delete account with active or pending bookings.");

                // Soft delete or hard delete based on your requirements
                _context.Students.Remove(student);
                _context.Users.Remove(student.User);

                await _context.SaveChangesAsync();

                return Ok("Account deleted successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error deleting account.");
            }
        }

      
        [HttpGet("get-current-theme")]
        public async Task<IActionResult> GetCurrentTheme()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var user = await _context.Users
                    .Where(u => u.UserId == userId)
                    .Select(u => new { u.ThemePreference })
                    .FirstOrDefaultAsync();

                if (user == null) return NotFound("User not found.");

                return Ok(new { themePreference = user.ThemePreference });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error getting current theme.");
            }
        }

        // Update the ToggleTheme method to accept theme parameter
        [HttpPost("toggle-theme")]
        public async Task<IActionResult> ToggleTheme([FromBody] ThemeDTO model)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var user = await _context.Users.FindAsync(userId);

                if (user == null) return NotFound("User not found.");

                // Use the provided theme or toggle the current one
                var newTheme = model?.Theme ?? (user.ThemePreference == "light" ? "dark" : "light");

                user.ThemePreference = newTheme;
                await _context.SaveChangesAsync();

                return Ok(new { theme = user.ThemePreference });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error updating theme.");
            }
        }

        // Add this DTO
        public class ThemeDTO
        {
            public string Theme { get; set; }
        }


    }
}