//using Microsoft.AspNetCore.Mvc;
//using TutorConnect.WebApp.Models;
//using TutorConnect.WebApp.Services;

//namespace TutorConnect.WebApp.Controllers
//{
//    public class StudentController : Controller
//    {
//        private readonly ApiService _api;

//        public StudentController(ApiService api)
//        {
//            _api = api;
//        }

//        public async Task<IActionResult> Dashboard()
//        {
//            try
//            {
//                var summary = await _api.GetStudentDashboardSummaryAsync();
//                return View(summary);
//            }
//            catch (HttpRequestException ex)
//            {
//                ViewBag.Error = ex.Message;
//                return View(new StudentDashboardSummaryDTO
//                {
//                    StudentName = "Student",
//                    AvailableTutors = 0,
//                    UpcomingBookings = 0
//                });
//            }
//        }

//        [HttpGet]
//        public async Task<IActionResult> BrowseTutors(string searchTerm = "", int? moduleId = null)
//        {
//            var tutors = await _api.GetTutorsAsync();
//            var modules = await _api.GetModulesAsync(); // ✅ Fetch all modules for dropdown

//            if (tutors == null)
//            {
//                ViewBag.Error = "Failed to load tutors.";
//                tutors = new List<TutorDTO>();
//            }

//            // Filter by search term
//            if (!string.IsNullOrWhiteSpace(searchTerm))
//            {
//                searchTerm = searchTerm.ToLower();
//                tutors = tutors.Where(t =>
//                    t.Name.ToLower().Contains(searchTerm) ||
//                    t.Surname.ToLower().Contains(searchTerm) ||
//                    t.Modules.Any(m => m.Name.ToLower().Contains(searchTerm) || m.Code.ToLower().Contains(searchTerm))
//                ).ToList();
//            }

//            // Filter by selected module
//            if (moduleId.HasValue)
//            {
//                tutors = tutors.Where(t => t.Modules.Any(m => m.ModuleId == moduleId.Value)).ToList();
//            }

//            ViewBag.SearchTerm = searchTerm;
//            ViewBag.ModuleId = moduleId;
//            ViewBag.Modules = modules;

//            return View(tutors);
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
    public class StudentController : Controller
    {
        private readonly ApiService _apiService;

        public StudentController(ApiService apiService)
        {
            _apiService = apiService;
        }

        [HttpGet]
        public async Task<IActionResult> BookSession()
        {
            var tutors = await _apiService.GetTutorsAsync();
            var modules = await _apiService.GetModulesAsync();

            ViewBag.Tutors = tutors.Select(t => new SelectListItem
            {
                Value = t.TutorId.ToString(),
                Text = t.Name
            }).ToList();

            ViewBag.Modules = modules.Select(m => new SelectListItem
            {
                Value = m.ModuleId.ToString(),
                Text = m.Name
            }).ToList();

            // ✅ Get UserId from JWT claim (the one you put inside token)
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized("UserId claim missing. Please re-login.");
            }

            int userId = int.Parse(userIdClaim.Value);

            // ✅ Ask API for the student record by UserId
            var student = await _apiService.GetStudentByUserIdAsync();
            if (student == null)
            {
                return Unauthorized("No student profile found for this user.");
            }

            var model = new BookingViewModel
            {
                StudentId = student.StudentId
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> BookSession(BookingViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var dto = new CreateBookingDTO
            {
                TutorId = model.TutorId,
                StudentId = model.StudentId,
                ModuleId = model.ModuleId,
                SessionDate = model.SessionDate,
                Notes = model.Notes
            };

            var success = await _apiService.CreateBookingAsync(dto);

            if (success)
            {
                ViewBag.Message = "Booking created successfully!";
                return RedirectToAction("MyBookings");
            }
            else
            {
                ViewBag.Error = "Failed to create booking.";
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> MyBookings()
        {
            // 1️⃣ Get UserId from JWT claim
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                TempData["ErrorMessage"] = "User information missing. Please log in again.";
                return RedirectToAction("Login", "Auth");
            }

            // 2️⃣ Fetch the student profile for this user
            var student = await _apiService.GetStudentByUserIdAsync();
            if (student == null)
            {
                TempData["ErrorMessage"] = "Student profile not found. Please contact support.";
                return RedirectToAction("Dashboard");
            }

            // 3️⃣ Fetch bookings using the correct StudentId
            var bookings = await _apiService.GetStudentBookingsAsync(student.StudentId);

            // 4️⃣ Pass the bookings to the view
            return View(bookings);
        }

        

        [HttpGet]
        public IActionResult Chat()
        {
            return View("~/Views/Shared/Chat.cshtml");
        }

    }
}


