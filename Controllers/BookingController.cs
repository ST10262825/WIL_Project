//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.Rendering;
//using TutorConnect.WebApp.Models;
//using TutorConnect.WebApp.Services;

//namespace TutorConnect.WebApp.Controllers
//{
//    public class BookingController : Controller
//    {
//        private readonly ApiService _apiService;

//        public BookingController(ApiService apiService)
//        {
//            _apiService = apiService;
//        }

//        // GET: Show booking form
//        [HttpGet]
//        public async Task<IActionResult> BookSession()
//        {
//            // Fetch tutors + modules for dropdowns
//            var tutors = await _apiService.GetTutorsAsync();
//            var modules = await _apiService.GetModulesAsync();

//            ViewBag.Tutors = new SelectList(tutors, "Id", "Name");
//            ViewBag.Modules = new SelectList(modules, "Id", "Name");

//            return View(new BookingViewModel());
//        }

//        // POST: Save booking
//        [HttpPost]
//        public async Task<IActionResult> BookSession(BookingViewModel model)
//        {
//            if (!ModelState.IsValid)
//            {
//                ViewBag.Error = "Invalid booking data.";
//                return View(model);
//            }

//            var dto = new CreateBookingDTO
//            {
//                StudentId = model.StudentId,
//                TutorId = model.TutorId,
//                ModuleId = model.ModuleId,
//                SessionDate = model.SessionDate,
//                Notes = model.Notes
//            };

//            var success = await _apiService.CreateBookingAsync(dto);

//            if (success)
//            {
//                TempData["Message"] = "Booking created successfully!";
//                return RedirectToAction("MyBookings", new { studentId = model.StudentId });
//            }

//            ViewBag.Error = "Failed to create booking.";
//            return View(model);
//        }

//        // GET: Student's bookings
//        [HttpGet]
//        public async Task<IActionResult> MyBookings(int studentId)
//        {
//            var bookings = await _apiService.GetStudentBookingsAsync(studentId);
//            return View(bookings);
//        }
//    }
//}


using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;
using TutorConnect.WebApp.Models;
using TutorConnect.WebApp.Services;

namespace TutorConnect.WebApp.Controllers
{
    public class BookingController : Controller
    {
        private readonly ApiService _apiService;

        public BookingController(ApiService apiService)
        {
            _apiService = apiService;
        }

        [HttpGet("Booking/Create/{tutorId}")]
        public async Task<IActionResult> Create(int tutorId)
        {
            try
            {
                var tutor = await _apiService.GetTutorByIdAsync(tutorId);
                if (tutor == null)
                {
                    TempData["Error"] = "Tutor not found.";
                    return RedirectToAction("Browse", "Tutor");
                }

                var modules = await _apiService.GetModulesAsync();

                ViewBag.Tutor = tutor;
                ViewBag.Modules = new SelectList(modules, "ModuleId", "Name"); // Use correct property names
                ViewBag.TutorId = tutorId;

                // Get actual student ID - FIX THIS PART
                var studentId = await GetActualStudentIdAsync();

                return View(new BookingViewModel
                {
                    TutorId = tutorId,
                    StudentId = studentId, // Use actual student ID
                    SelectedDate = DateTime.Today
                });
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error loading booking page. Please try again.";
                return RedirectToAction("Browse", "Tutor");
            }
        }

        private async Task<int> GetActualStudentIdAsync()
        {
            try
            {
                // First try to get from claims if available
                var studentIdClaim = User.FindFirst("StudentId")?.Value;
                if (!string.IsNullOrEmpty(studentIdClaim) && int.TryParse(studentIdClaim, out int studentId))
                {
                    return studentId;
                }

                // If not in claims, get from API service
                var student = await _apiService.GetStudentByUserIdAsync();
                if (student != null && student.StudentId > 0)
                {
                    return student.StudentId;
                }

                // If all else fails, try to get from NameIdentifier claim
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int userId))
                {
                    // You might need to create an API endpoint to get student by user ID
                    var studentByUser = await _apiService.GetStudentByUserIdAsync(userId);
                    if (studentByUser != null)
                    {
                        return studentByUser.StudentId;
                    }
                }

                throw new Exception("Could not determine student ID. Please ensure you're logged in as a student.");
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"Error getting student ID: {ex.Message}");
                throw;
            }
        }

        // POST: Create booking
        [HttpPost("Booking/Create/{tutorId}")]
        public async Task<IActionResult> Create(int tutorId, BookingViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Reload necessary data for the view
                var tutor = await _apiService.GetTutorByIdAsync(tutorId);
                var modules = await _apiService.GetModulesAsync();

                ViewBag.Tutor = tutor;
                ViewBag.Modules = new SelectList(modules, "ModuleId", "Name");
                return View(model);
            }

            try
            {
                var dto = new CreateBookingDTO
                {
                    TutorId = model.TutorId,
                    StudentId = model.StudentId,
                    ModuleId = model.ModuleId,
                    StartTime = model.StartTime,
                    EndTime = model.EndTime,
                    Notes = model.Notes
                };

                var success = await _apiService.CreateBookingAsync(dto);

                if (success)
                {
                    TempData["Message"] = "Booking created successfully!";
                    return RedirectToAction("MyBookings"); // Remove studentId parameter
                }

                ViewBag.Error = "Failed to create booking. Please try again.";
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"Booking creation error: {ex.Message}");
                ViewBag.Error = "An error occurred while creating the booking. Please try again.";
            }

            // Reload view data if there was an error
            var tutorError = await _apiService.GetTutorByIdAsync(tutorId);
            var modulesError = await _apiService.GetModulesAsync();

            ViewBag.Tutor = tutorError;
            ViewBag.Modules = new SelectList(modulesError, "ModuleId", "Name");

            return View(model);
        }


        // GET: Fetch availability for tutor (AJAX call)
        [HttpGet("Booking/Availability/{tutorId}")]
        public async Task<IActionResult> Availability(int tutorId, DateTime date)
        {
            var slots = await _apiService.GetTutorAvailabilityAsync(tutorId, date);
            return Json(slots);
        }

        // GET: Student's bookings
        [HttpGet]
        public async Task<IActionResult> MyBookings()
        {
            try
            {
                // Get the logged-in student's ID
                var studentId = await GetActualStudentIdAsync(); // Use async method

                var bookings = await _apiService.GetStudentBookingsAsync(studentId);
                return View(bookings);
            }
            catch (Exception ex)
            {
                // Handle error (e.g., user not logged in)
                TempData["Error"] = "Please log in to view your bookings.";
                return RedirectToAction("Login", "Account");
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateBookingStatus([FromBody] BookingStatusUpdateModel dto)
        {
            try
            {
                var success = await _apiService.UpdateBookingStatusAsync(dto.BookingId, dto.Status);
                if (!success) return BadRequest(new { message = "Could not update booking status." });
                return Ok();
            }
            catch
            {
                return StatusCode(500, new { message = "Server error updating booking status." });
            }
        }



    }
}
