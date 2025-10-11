using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;
using TutorConnect.WebApp.Models;
using TutorConnect.WebApp.Services;

namespace TutorConnect.WebApp.Controllers
{
    public class StudentController : Controller
    {
        private readonly ApiService _apiService;

        public StudentController(ApiService apiService)
        {
            _apiService = apiService;
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                // Get logged-in student
                var student = await _apiService.GetStudentByUserIdAsync();
                if (student == null)
                {
                    TempData["Error"] = "Please log in to access the dashboard.";
                    return RedirectToAction("Login", "Account");
                }

                // Get student bookings with timeout
                var bookings = await GetWithTimeout(_apiService.GetStudentBookingsAsync(student.StudentId), 5000);

                // Get dashboard summary with timeout
                var summary = await GetWithTimeout(_apiService.GetStudentDashboardSummaryAsync(), 5000);

                
                var upcomingSessions = bookings?.Where(b => b.StartTime >= DateTime.Today && b.Status == "Accepted").ToList() ?? new List<BookingDTO>();
                ViewBag.UpcomingSessionsWithAccess = upcomingSessions;

                // Get unread messages count
                var unreadMessagesCount = 0;
                try
                {
                    unreadMessagesCount = await _apiService.GetUnreadMessagesCountAsync(student.StudentId);
                }
                catch
                {
                    // Silently fail for messages count - it's not critical
                    unreadMessagesCount = 0;
                }

                // Get pending reviews count
                var pendingReviews = await _apiService.GetPendingReviewsAsync(student.StudentId);

                ViewBag.Student = student;
                ViewBag.UpcomingSessions = bookings?.Where(b => b.StartTime >= DateTime.Today &&
                                                              (b.Status == "Accepted" || b.Status == "Pending"))
                                                   .OrderBy(b => b.StartTime)
                                                   .ToList() ?? new List<BookingDTO>();

                ViewBag.PendingCount = bookings?.Count(b => b.Status == "Pending") ?? 0;
                ViewBag.CompletedCount = bookings?.Count(b => b.Status == "Completed") ?? 0;
                ViewBag.TotalHours = summary?.TotalLearningHours ?? 0;
                ViewBag.ActiveTutors = summary?.ActiveTutorsCount ?? 0;
                ViewBag.UnreadMessagesCount = unreadMessagesCount;
                ViewBag.PendingReviewsCount = pendingReviews.Count; // Add this line

                return View(bookings ?? new List<BookingDTO>());
            }
            catch (TimeoutException)
            {
                TempData["Error"] = "The request timed out. Please try again.";
                return View(new List<BookingDTO>());
            }
            catch (Exception ex)
            {
                // Log the actual error
                Console.WriteLine($"Dashboard error: {ex.Message}");
                TempData["Error"] = "Error loading dashboard. Showing limited information.";
                return View(new List<BookingDTO>());
            }
        }

        private async Task<T> GetWithTimeout<T>(Task<T> task, int timeoutMilliseconds)
        {
            var timeoutTask = Task.Delay(timeoutMilliseconds);
            var completedTask = await Task.WhenAny(task, timeoutTask);

            if (completedTask == timeoutTask)
            {
                throw new TimeoutException("The operation has timed out.");
            }

            return await task;
        }


        [HttpPost]
        public async Task<IActionResult> UpdateProfile(int studentId, string bio, IFormFile profileImage)
        {
            try
            {
                var result = await _apiService.UpdateStudentProfileAsync(studentId, bio, profileImage);

                if (result.Success)
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        // AJAX request - return JSON
                        return Json(new
                        {
                            success = true,
                            message = result.Message,
                            profileImageUrl = result.ProfileImageUrl,
                            bio = bio
                        });
                    }
                    else
                    {
                        // Regular form submission
                        TempData["Message"] = result.Message;
                        return RedirectToAction("Dashboard");
                    }
                }
                else
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = false, message = result.Message });
                    }
                    else
                    {
                        TempData["Error"] = result.Message;
                        return RedirectToAction("Dashboard");
                    }
                }
            }
            catch (Exception ex)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Error updating profile." });
                }
                else
                {
                    TempData["Error"] = "Error updating profile.";
                    return RedirectToAction("Dashboard");
                }
            }
        }


       [HttpGet]
public async Task<IActionResult> MySessions()
{
    try
    {
        var student = await _apiService.GetStudentByUserIdAsync();
        if (student == null) return RedirectToAction("Login", "Account");

        var bookings = await _apiService.GetStudentBookingsAsync(student.StudentId);
        
        // Create a list to store booking review status
        var bookingsWithReviewStatus = new List<BookingDTO>();

                foreach (var booking in bookings)
                {
                    bookingsWithReviewStatus.Add(new BookingDTO
                    {
                        BookingId = booking.BookingId,
                        TutorId = booking.TutorId,
                        TutorName = booking.TutorName,
                        StudentId = booking.StudentId,
                        StudentName = booking.StudentName,
                        ModuleName = booking.ModuleName,
                        StartTime = booking.StartTime,
                        EndTime = booking.EndTime,
                        Status = booking.Status,
                        Notes = booking.Notes,
                        HasBeenReviewed = booking.Status == "Completed" ?
                            await _apiService.HasBookingBeenReviewedAsync(booking.BookingId) : false
                    });
                }


                return View(bookingsWithReviewStatus);
    }
    catch (Exception ex)
    {
        TempData["Error"] = "Error loading sessions.";
        return RedirectToAction("Dashboard");
    }
}



        [HttpPost]
        public async Task<IActionResult> CancelBooking(int bookingId)
        {
            try
            {
                var success = await _apiService.UpdateBookingStatusAsync(bookingId, "Declined");
                if (success)
                {
                    TempData["Message"] = "Booking cancelled successfully.";
                }
                else
                {
                    TempData["Error"] = "Failed to cancel booking.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error cancelling booking.";
            }

            return RedirectToAction("MySessions");
        }

        private int GetLoggedInStudentId()
        {
            var studentIdClaim = User.FindFirst("StudentId")?.Value;
            if (int.TryParse(studentIdClaim, out int studentId))
            {
                return studentId;
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int userId))
            {
                // You might need to get student ID from user ID
                return userId; // This is temporary
            }

            throw new Exception("Student not authenticated");
        }

        [HttpGet]
        public IActionResult Chat()
        {
            return View("~/Views/Shared/Chat.cshtml");
        }



        [HttpGet]
        public async Task<IActionResult> Reviews()
        {
            try
            {
                var student = await _apiService.GetStudentByUserIdAsync();
                if (student == null) return RedirectToAction("Login", "Account");

                var pendingReviews = await _apiService.GetPendingReviewsAsync(student.StudentId);

                // Store count for sidebar badge
                ViewBag.PendingReviewsCount = pendingReviews.Count;

                return View(pendingReviews);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error loading reviews.";
                return RedirectToAction("Dashboard");
            }
        }

        [HttpGet("Student/Reviews/{tutorId}")]
        public async Task<IActionResult> ReviewTutor(int tutorId)
        {
            var student = await _apiService.GetStudentByUserIdAsync();
            if (student == null) return RedirectToAction("Login", "Account");

            // Fetch pending reviews for this student
            var pendingReviews = await _apiService.GetPendingReviewsAsync(student.StudentId);

            // Filter for this specific tutor
            var review = pendingReviews.FirstOrDefault(r => r.TutorId == tutorId);
            if (review == null)
            {
                TempData["Error"] = "No review pending for this tutor.";
                return RedirectToAction("MySessions");
            }

            return View("TutorReview", review); // New view for single tutor review
        }


        [HttpPost]
        public async Task<IActionResult> SubmitReview([FromBody] CreateReviewDTO reviewDto)
        {
            try
            {
                var student = await _apiService.GetStudentByUserIdAsync();
                if (student == null) return Json(new { success = false, message = "Student not found" });

                reviewDto.StudentId = student.StudentId;

                var success = await _apiService.SubmitReviewAsync(reviewDto);

                if (success)
                {
                    return Json(new { success = true, message = "Review submitted successfully!" });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to submit review." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error submitting review." });
            }
        }



        [HttpGet]
        public async Task<IActionResult> Materials()
        {
            try
            {
                Console.WriteLine("[WEBAPP] Student Materials page accessed");

                var student = await _apiService.GetStudentByUserIdAsync();
                if (student == null)
                {
                    Console.WriteLine("[WEBAPP] Student not found - redirecting to login");
                    return RedirectToAction("Login", "Account");
                }

                Console.WriteLine($"[WEBAPP] Student found: ID={student.StudentId}, Name={student.Name}");

                // Get materials overview (now strongly-typed)
                Console.WriteLine("[WEBAPP] Calling GetStudentMaterialsOverviewAsync...");
                var materialsOverview = await _apiService.GetStudentMaterialsOverviewAsync(student.StudentId);
                Console.WriteLine($"[WEBAPP] Materials overview - TotalMaterials: {materialsOverview?.TotalMaterials}, TotalTutors: {materialsOverview?.TotalTutors}, Tutors Count: {materialsOverview?.Tutors?.Count}");

                // Get all accessible materials
                Console.WriteLine("[WEBAPP] Calling GetStudentMaterialsAsync...");
                var materials = await _apiService.GetStudentMaterialsAsync(student.StudentId);
                Console.WriteLine($"[WEBAPP] Materials received: Count={materials?.Count ?? 0}");

                ViewBag.StudentId = student.StudentId;
                ViewBag.StudentName = student.Name;
                ViewBag.MaterialsOverview = materialsOverview; // This is now strongly-typed

                return View(materials ?? new List<LearningMaterialDTO>());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WEBAPP] ERROR in Materials: {ex.Message}");
                Console.WriteLine($"[WEBAPP] Stack trace: {ex.StackTrace}");
                TempData["Error"] = "Error loading learning materials.";
                return RedirectToAction("Dashboard");
            }
        }



        [HttpGet("Student/TutorMaterials/{tutorId}")]
        public async Task<IActionResult> TutorMaterials(int tutorId)
        {
            try
            {
                var student = await _apiService.GetStudentByUserIdAsync();
                if (student == null) return RedirectToAction("Login", "Account");

                var materials = await _apiService.GetTutorMaterialsForStudentAsync(student.StudentId, tutorId);

                // Get tutor info for display
                var tutor = await _apiService.GetTutorByIdAsync(tutorId);

                ViewBag.TutorId = tutorId;
                ViewBag.TutorName = tutor?.Name + " " + tutor?.Surname;
                ViewBag.StudentId = student.StudentId;

                return View(materials ?? new List<LearningMaterialDTO>());
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error loading tutor materials.";
                return RedirectToAction("Materials");
            }
        }

        // Update these methods in your StudentController (WebApp)
       
        [HttpGet]
        public async Task<IActionResult> Settings()
        {
            try
            {
                var student = await _apiService.GetStudentByUserIdAsync();
                if (student == null) return RedirectToAction("Login", "Auth");

                // FIX: Add parentheses to call the method
                var currentTheme = await _apiService.GetCurrentThemeAsync();
                ViewBag.CurrentTheme = currentTheme ?? "light";

                return View();
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error loading settings.";
                return RedirectToAction("Dashboard");
            }
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword()
        {
            try
            {
                Console.WriteLine($"[WebApp Controller] ChangePassword called");

                // Read the raw request body
                using var reader = new StreamReader(Request.Body);
                var rawRequestBody = await reader.ReadToEndAsync();
                Console.WriteLine($"[WebApp Controller] Raw request body: {rawRequestBody}");

                // Try to parse as JSON
                ChangePasswordDTO dto = null;
                try
                {
                    dto = System.Text.Json.JsonSerializer.Deserialize<ChangePasswordDTO>(rawRequestBody, new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    Console.WriteLine($"[WebApp Controller] Deserialized DTO - Current: '{dto?.CurrentPassword}', New: '{dto?.NewPassword}', Confirm: '{dto?.ConfirmPassword}'");
                }
                catch (Exception jsonEx)
                {
                    Console.WriteLine($"[WebApp Controller] JSON deserialization failed: {jsonEx.Message}");
                }

                // If deserialization failed, try manual parsing
                if (dto == null || string.IsNullOrEmpty(dto.CurrentPassword))
                {
                    Console.WriteLine($"[WebApp Controller] Attempting manual JSON parsing...");

                    // Manual parsing as fallback
                    using var jsonDoc = System.Text.Json.JsonDocument.Parse(rawRequestBody);
                    var root = jsonDoc.RootElement;

                    dto = new ChangePasswordDTO
                    {
                        CurrentPassword = root.TryGetProperty("CurrentPassword", out var currentProp) ? currentProp.GetString() :
                                         root.TryGetProperty("currentPassword", out var currentLower) ? currentLower.GetString() : null,
                        NewPassword = root.TryGetProperty("NewPassword", out var newProp) ? newProp.GetString() :
                                     root.TryGetProperty("newPassword", out var newLower) ? newLower.GetString() : null,
                        ConfirmPassword = root.TryGetProperty("ConfirmPassword", out var confirmProp) ? confirmProp.GetString() :
                                         root.TryGetProperty("confirmPassword", out var confirmLower) ? confirmLower.GetString() : null
                    };

                    Console.WriteLine($"[WebApp Controller] Manually parsed - Current: '{dto.CurrentPassword}', New: '{dto.NewPassword}', Confirm: '{dto.ConfirmPassword}'");
                }

                var student = await _apiService.GetStudentByUserIdAsync();
                if (student == null) return Json(new { success = false, message = "Student not found" });

                if (string.IsNullOrEmpty(dto.CurrentPassword))
                {
                    Console.WriteLine($"[WebApp Controller] VALIDATION FAILED: CurrentPassword is null or empty");
                    return Json(new { success = false, message = "Current password is required." });
                }

                if (string.IsNullOrEmpty(dto.NewPassword) || dto.NewPassword.Length < 6)
                {
                    return Json(new { success = false, message = "New password must be at least 6 characters long." });
                }

                if (dto.NewPassword != dto.ConfirmPassword)
                {
                    return Json(new { success = false, message = "New password and confirmation do not match." });
                }

                Console.WriteLine($"[WebApp Controller] Calling API service...");
                var success = await _apiService.ChangePasswordAsync(dto);

                if (success)
                {
                    return Json(new { success = true, message = "Password changed successfully!" });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to change password. Please check your current password." });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WebApp Controller] Exception: {ex.Message}");
                Console.WriteLine($"[WebApp Controller] Stack: {ex.StackTrace}");
                return Json(new { success = false, message = "Error changing password. Please try again." });
            }
        }



        [HttpPost]
        public async Task<IActionResult> DeleteAccount(string confirmation)
        {
            try
            {
                if (confirmation?.ToLower() != "delete my account")
                {
                    return Json(new { success = false, message = "Please type 'delete my account' to confirm." });
                }

                var student = await _apiService.GetStudentByUserIdAsync();
                if (student == null) return Json(new { success = false, message = "Student not found" });

                var success = await _apiService.DeleteAccountAsync();
                if (success)
                {
                    await HttpContext.SignOutAsync("Cookies");
                    return Json(new
                    {
                        success = true,
                        message = "Account deleted successfully.",
                        redirectUrl = Url.Action("Login", "Auth")
                    });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to delete account. Please try again." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting account: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ToggleTheme()
        {
            try
            {
                var student = await _apiService.GetStudentByUserIdAsync();
                if (student == null) return Json(new { success = false, message = "Student not found" });

                var success = await _apiService.ToggleThemeAsync();
                if (success)
                {
                    // Get the new theme after toggling
                    var newTheme = await _apiService.GetCurrentThemeAsync();

                    // Set theme cookie
                    Response.Cookies.Append("ThemePreference", newTheme ?? "light", new CookieOptions
                    {
                        Expires = DateTimeOffset.Now.AddYears(1),
                        Path = "/"
                    });

                    return Json(new
                    {
                        success = true,
                        message = "Theme preference updated!",
                        theme = newTheme,
                        reload = true
                    });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to update theme." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error updating theme: " + ex.Message });
            }
        }



    }
}





