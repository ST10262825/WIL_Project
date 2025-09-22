using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using TutorConnect.WebApp.Models;
using TutorConnect.WebApp.Services;

namespace TutorConnect.WebApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminDashboardController : Controller
    {
        private readonly ApiService _apiService;
        private readonly ILogger<AdminDashboardController> _logger;

        public AdminDashboardController(ApiService apiService, ILogger<AdminDashboardController> logger)
        {
            _apiService = apiService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var stats = await _apiService.GetAsync<dynamic>("api/admin/dashboard-stats");
                ViewBag.Stats = stats;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin dashboard");
                TempData["Error"] = "Error loading dashboard statistics";
                return View();
            }
        }

        // Tutor Management Views
        public async Task<IActionResult> Tutors(int page = 1)
        {
            try
            {
                var tutors = await _apiService.GetAsync<dynamic>($"api/admin/tutors?page={page}&pageSize=10");
                return View(tutors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading tutors");
                TempData["Error"] = "Error loading tutors";
                return View(new { Tutors = new List<object>(), TotalPages = 1, Page = 1 });
            }
        }

        // Student Management Views
        public async Task<IActionResult> Students(int page = 1)
        {
            try
            {
                var students = await _apiService.GetAsync<dynamic>($"api/admin/students?page={page}&pageSize=10");
                return View(students);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading students");
                TempData["Error"] = "Error loading students";
                return View(new { Students = new List<object>(), TotalPages = 1, Page = 1 });
            }
        }

        // Booking Management Views
        public async Task<IActionResult> Bookings(int page = 1, string status = "all")
        {
            try
            {
                var bookings = await _apiService.GetAsync<dynamic>($"api/admin/bookings?page={page}&pageSize=10&status={status}");
                ViewBag.StatusFilter = status;
                return View(bookings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading bookings");
                TempData["Error"] = "Error loading bookings";
                return View(new { Bookings = new List<object>(), TotalPages = 1, Page = 1 });
            }
        }

        // Module Management Views
        public async Task<IActionResult> Modules()
        {
            try
            {
                var modules = await _apiService.GetAsync<List<ModuleDTO>>("api/admin/modules");
                return View(modules);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading modules");
                TempData["Error"] = "Error loading modules";
                return View(new List<ModuleDTO>());
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateModule(CreateModuleViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View("Modules", await _apiService.GetAsync<List<ModuleDTO>>("api/admin/modules"));
                }

                var result = await _apiService.PostAsync<dynamic>("api/admin/modules", model);
                TempData["Success"] = "Module created successfully!";
                return RedirectToAction("Modules");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error creating module");
                TempData["Error"] = ex.Message.Contains("already exists")
                    ? "A module with this code or name already exists."
                    : "Error creating module";
                return RedirectToAction("Modules");
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteModule(int id)
        {
            try
            {
                await _apiService.DeleteAsync($"api/admin/modules/{id}");
                TempData["Success"] = "Module deleted successfully!";
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error deleting module");
                TempData["Error"] = ex.Message.Contains("associated bookings")
                    ? "Cannot delete module with associated bookings."
                    : "Error deleting module";
            }
            return RedirectToAction("Modules");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteTutor(int id)
        {
            try
            {
                await _apiService.DeleteAsync($"api/admin/tutors/{id}");
                TempData["Success"] = "Tutor deleted successfully!";
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error deleting tutor");
                TempData["Error"] = ex.Message.Contains("active or pending bookings")
                    ? "Cannot delete tutor with active bookings. Please cancel bookings first."
                    : "Error deleting tutor";
            }
            return RedirectToAction("Tutors");
        }

        [HttpPost]
        public async Task<IActionResult> BlockStudent(int id)
        {
            try
            {
                await _apiService.PostAsync<object>($"api/admin/students/{id}/block", null);
                TempData["Success"] = "Student blocked successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error blocking student");
                TempData["Error"] = "Error blocking student";
            }
            return RedirectToAction("Students");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteBooking(int id)
        {
            try
            {
                await _apiService.DeleteAsync($"api/admin/bookings/{id}");
                TempData["Success"] = "Booking deleted successfully!";
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error deleting booking");
                TempData["Error"] = ex.Message.Contains("completed bookings")
                    ? "Cannot delete completed bookings."
                    : "Error deleting booking";
            }
            return RedirectToAction("Bookings");
        }

        [HttpPost]
        public async Task<IActionResult> CleanupOldBookings()
        {
            try
            {
                var result = await _apiService.PostAsync<dynamic>("api/admin/maintenance/cleanup-old-bookings", null);
                TempData["Success"] = result?.ToString() ?? "Cleanup completed successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up old bookings");
                TempData["Error"] = "Error performing cleanup";
            }
            return RedirectToAction("Index");
        }
    }

    
}