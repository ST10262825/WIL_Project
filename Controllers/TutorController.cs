using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TutorConnect.WebApp.Models;
using TutorConnect.WebApp.Services;

namespace TutorConnect.WebApp.Controllers
{
    public class TutorController : Controller
    {
        private readonly ApiService _apiService;
        private readonly IWebHostEnvironment _env;

        public TutorController(ApiService apiService, IWebHostEnvironment env)
        {
            _apiService = apiService;
            _env = env;
        }

        // -------------------------
        // Dashboard
        // -------------------------
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                TempData["ErrorMessage"] = "User information missing. Please log in again.";
                return RedirectToAction("Login", "Auth");
            }

            var tutor = await _apiService.GetTutorByUserIdAsync(userId);
            if (tutor == null)
            {
                TempData["ErrorMessage"] = "Tutor profile not found.";
                return RedirectToAction("Login", "Auth");
            }

            var bookings = await _apiService.GetTutorBookingsAsync(tutor.TutorId);
            var unreadMessagesCount = await _apiService.GetUnreadMessagesCountAsync(userId);

            ViewBag.TutorName = tutor.Name + " " + tutor.Surname;
            ViewBag.TutorBio = tutor.Bio ?? "No bio set";
            ViewBag.UnreadMessagesCount = unreadMessagesCount;

            // ✅ Avoid caching profile images
            ViewBag.TutorProfileImageUrl = string.IsNullOrEmpty(tutor.ProfileImageUrl)
                ? Url.Content("~/images/default-profile.png")
                : $"{tutor.ProfileImageUrl}?v={DateTime.UtcNow.Ticks}";

            return View(bookings);
        }

        // -------------------------
        // Update booking status
        // -------------------------
        [HttpPost]
        public async Task<IActionResult> UpdateBookingStatus(int bookingId, string status, string? reason = null)
        {
            var success = await _apiService.UpdateSessionStatusAsync(bookingId, status, reason);
            if (!success)
                TempData["ErrorMessage"] = "Failed to update booking status.";

            return RedirectToAction("Dashboard");
        }

        // -------------------------
        // Chat
        // -------------------------
        [HttpGet]
        public IActionResult Chat()
        {
            return View("~/Views/Shared/Chat.cshtml");
        }

        // -------------------------
        // MyProfile (GET)
        // -------------------------
        [HttpGet]
        public async Task<IActionResult> MyProfile()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
            {
                TempData["ErrorMessage"] = "User info missing. Please log in again.";
                return RedirectToAction("Login", "Auth");
            }

            var tutor = await _apiService.GetTutorByUserIdAsync(userId);
            if (tutor == null) return NotFound();

            var model = new TutorProfileViewModel
            {
                TutorId = tutor.TutorId,
                Name = tutor.Name + " " + tutor.Surname,
                Bio = tutor.Bio,
                AboutMe = tutor.AboutMe,
                Expertise = tutor.Expertise,
                Education = tutor.Education,
                ProfileImageUrl = tutor.ProfileImageUrl
            };

            return View(model);
        }

        // -------------------------
        // MyProfile (POST)
        // -------------------------
        [HttpPost]
        public async Task<IActionResult> MyProfile(TutorProfileViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Call the API with all fields
            var (IsSuccess, ErrorMessage, ProfileImageUrl) = await _apiService.UpdateTutorProfileAsync(
                model.TutorId,
                model.Bio,
                model.AboutMe,
                model.Expertise,
                model.Education,
                model.ProfileImage
            );

            if (!IsSuccess)
            {
                TempData["ErrorMessage"] = $"Failed to update profile: {ErrorMessage}";
                return View(model);
            }

            TempData["SuccessMessage"] = "Profile updated successfully!";

            // ✅ Append timestamp to new image URL to force reload
            if (!string.IsNullOrEmpty(ProfileImageUrl))
            {
                ViewBag.TutorProfileImageUrl = $"{ProfileImageUrl}?v={DateTime.UtcNow.Ticks}";
            }

            return RedirectToAction("Dashboard");
        }

        //[HttpGet]
        //public async Task<IActionResult> BrowseTutors(string? subject)
        //{
        //    // Fetch all tutors from API
        //    var tutorsFromApi = await _apiService.GetTutorsAsync();

        //    // Optional filtering by subject/module
        //    if (!string.IsNullOrEmpty(subject) && subject != "All Subjects")
        //    {
        //        tutorsFromApi = tutorsFromApi
        //            .Where(t => t.Modules.Any(m => m.Name.Equals(subject, StringComparison.OrdinalIgnoreCase)))
        //            .ToList();
        //    }

        //    // Map to ViewModel
        //    var tutors = tutorsFromApi.Select(t => new TutorProfileViewModel
        //    {
        //        TutorId = t.TutorId,
        //        Name = t.Name + " " + t.Surname,
        //        Bio = t.Bio,
        //        ProfileImageUrl = string.IsNullOrEmpty(t.ProfileImageUrl)
        //            ? Url.Content("~/images/default-profile.png")
        //            : $"{t.ProfileImageUrl}?v={DateTime.UtcNow.Ticks}", // ensures updated image
        //        Expertise = t.Modules != null
        //            ? string.Join(", ", t.Modules.Select(m => m.Name))
        //            : ""
        //    }).ToList();

        //    // Pass the subject filter for the dropdown
        //    ViewBag.SelectedSubject = subject ?? "All Subjects";

        //    return View(tutors);
        //}

        [HttpGet]
        public async Task<IActionResult> Browse()
        {
            var tutors = await _apiService.GetAllTutorsAsync();

            // Optional: pass subjects for filtering dropdown
            var allSubjects = tutors.SelectMany(t => t.Subjects).Distinct().OrderBy(s => s).ToList();
            ViewBag.Subjects = allSubjects;

            return View(tutors);
        }

       

    }
}
