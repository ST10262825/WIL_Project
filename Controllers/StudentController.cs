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
                return View(bookings);
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
    }
}





