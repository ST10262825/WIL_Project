using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using TutorConnect.WebApp.Models;
using TutorConnect.WebApp.Services;
using System.Security.Claims;

namespace TutorConnect.WebApp.Controllers
{
    public class StudentDashboardController : Controller
    {
        private readonly ApiService _apiService;

        public StudentDashboardController(ApiService apiService)
        {
            _apiService = apiService;
        }

        // GET: /StudentDashboard/BookSession
        public async Task<IActionResult> BookSession()
        {
            var tutors = await _apiService.GetTutorsAsync(); // from API admin controller
            var modules = await _apiService.GetModulesAsync(); // you need this API method, or you can load from tutors' modules

            var model = new BookSessionViewModel
            {
                AvailableTutors = tutors,
                AvailableModules = modules,
                StartTime = DateTime.Now.AddHours(1), // default start time
                EndTime = DateTime.Now.AddHours(2) // default end time
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var studentIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (studentIdClaim == null) return Unauthorized();

            int userId = int.Parse(studentIdClaim); // from JWT
            var student = await _apiService.GetStudentByUserIdAsync(userId); // new method needed
            if (student == null) return Unauthorized();

            int studentId = student.Id;

            var upcomingSessions = await _apiService.GetStudentSessionsAsync(studentId);

            return View(upcomingSessions); // Pass to the view
        }


        // POST: /StudentDashboard/BookSession
        [HttpPost]
        public async Task<IActionResult> BookSession(BookSessionViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Repopulate dropdowns on error
                model.AvailableTutors = await _apiService.GetTutorsAsync();
                model.AvailableModules = await _apiService.GetModulesAsync();
                return View(model);
            }

            // Get logged-in student ID (assuming stored in claims)
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            int userId = int.Parse(userIdClaim);
            var student = await _apiService.GetStudentByUserIdAsync(userId);
            if (student == null)
            {
                return Unauthorized(); // or maybe redirect with error
            }

            model.StudentId = student.Id;


            // Map to API DTO
            var bookingDto = new SessionBookingDTO
            {
                StudentId = model.StudentId,
                TutorId = model.TutorId,
                ModuleId = model.ModuleId,
                StartTime = model.StartTime,
                EndTime = model.EndTime
            };

            var success = await _apiService.BookSessionAsync(bookingDto);
            if (!success)
            {
                ModelState.AddModelError("", "Failed to book the session. Please try again.");
                model.AvailableTutors = await _apiService.GetTutorsAsync();
                model.AvailableModules = await _apiService.GetModulesAsync();
                return View(model);
            }

            TempData["SuccessMessage"] = "Session booked successfully!";
            return RedirectToAction("Index", "StudentDashboard");
        }
    }
}
