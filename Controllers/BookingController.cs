using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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

        // GET: Show booking form
        [HttpGet]
        public async Task<IActionResult> BookSession()
        {
            // Fetch tutors + modules for dropdowns
            var tutors = await _apiService.GetTutorsAsync();
            var modules = await _apiService.GetModulesAsync();

            ViewBag.Tutors = new SelectList(tutors, "Id", "Name");
            ViewBag.Modules = new SelectList(modules, "Id", "Name");

            return View(new BookingViewModel());
        }

        // POST: Save booking
        [HttpPost]
        public async Task<IActionResult> BookSession(BookingViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Error = "Invalid booking data.";
                return View(model);
            }

            var dto = new CreateBookingDTO
            {
                StudentId = model.StudentId,
                TutorId = model.TutorId,
                ModuleId = model.ModuleId,
                SessionDate = model.SessionDate,
                Notes = model.Notes
            };

            var success = await _apiService.CreateBookingAsync(dto);

            if (success)
            {
                TempData["Message"] = "Booking created successfully!";
                return RedirectToAction("MyBookings", new { studentId = model.StudentId });
            }

            ViewBag.Error = "Failed to create booking.";
            return View(model);
        }

        // GET: Student's bookings
        [HttpGet]
        public async Task<IActionResult> MyBookings(int studentId)
        {
            var bookings = await _apiService.GetStudentBookingsAsync(studentId);
            return View(bookings);
        }
    }
}
