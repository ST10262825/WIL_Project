using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;
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
            var userId = GetUserId();
            if (userId == null) return RedirectToLogin();

            var tutor = await _apiService.GetTutorByUserIdAsync(userId.Value);
            if (tutor == null) return RedirectToLogin("Tutor profile not found.");

            var bookings = await _apiService.GetTutorBookingsAsync(tutor.TutorId);
            var unreadMessagesCount = await _apiService.GetUnreadMessagesCountAsync(userId.Value);

            ViewBag.TutorName = $"{tutor.Name} {tutor.Surname}";
            ViewBag.TutorBio = tutor.Bio ?? "No bio set";
            ViewBag.UnreadMessagesCount = unreadMessagesCount;

            ViewBag.TutorProfileImageUrl = string.IsNullOrEmpty(tutor.ProfileImageUrl)
                ? Url.Content("~/images/default-profile.png")
                : $"{tutor.ProfileImageUrl}?v={DateTime.UtcNow.Ticks}";

            return View(bookings);
        }

        // -------------------------
        // MyProfile (GET)
        // -------------------------
        [HttpGet]
        public async Task<IActionResult> MyProfile()
        {
            var userId = GetUserId();
            if (userId == null) return RedirectToLogin();

            var tutor = await _apiService.GetTutorByUserIdAsync(userId.Value);
            if (tutor == null) return NotFound();

            var model = MapTutorToProfileViewModel(tutor);
            return View(model);
        }

        // -------------------------
        // MyProfile (POST)
        // -------------------------
        [HttpPost]
        public async Task<IActionResult> MyProfile(TutorProfileViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // Convert ExpertiseList to comma-separated string
            var expertiseCsv = model.ExpertiseList != null ? string.Join(", ", model.ExpertiseList) : "";

            // Convert EducationList to JSON string
            var educationJson = model.EducationList != null
                ? JsonSerializer.Serialize(model.EducationList)
                : "[]";

            var (IsSuccess, ErrorMessage, ProfileImageUrl) = await _apiService.UpdateTutorProfileAsync(
                model.TutorId,
                model.Bio,
                model.AboutMe,
                expertiseCsv,
                educationJson,
                model.ProfileImage
            );

            if (!IsSuccess)
            {
                TempData["ErrorMessage"] = $"Failed to update profile: {ErrorMessage}";
                return View(model);
            }

            TempData["SuccessMessage"] = "Profile updated successfully!";
            if (!string.IsNullOrEmpty(ProfileImageUrl))
                ViewBag.TutorProfileImageUrl = $"{ProfileImageUrl}?v={DateTime.UtcNow.Ticks}";

            return RedirectToAction("Dashboard");
        }

        // -------------------------
        // Profile view by TutorId
        // -------------------------
        [HttpGet("/Tutor/Profile/{tutorId}")]
        public async Task<IActionResult> Profile(int tutorId)
        {
            var tutor = await _apiService.GetTutorByIdAsync(tutorId);
            if (tutor == null) return NotFound();

            var model = MapTutorToProfileViewModel(tutor);
            return View(model);
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
        // Browse Tutors
        // -------------------------
        [HttpGet]
        public async Task<IActionResult> Browse()
        {
            var tutors = await _apiService.GetAllTutorsAsync();
            ViewBag.Subjects = tutors.SelectMany(t => t.Subjects).Distinct().OrderBy(s => s).ToList();
            return View(tutors);
        }

        // -------------------------
        // Helper: Map TutorDTO to TutorProfileViewModel
        // -------------------------
        private TutorProfileViewModel MapTutorToProfileViewModel(TutorDTO tutor)
        {
            // Convert Expertise CSV to list
            // In MapTutorToProfileViewModel
            var expertiseList = !string.IsNullOrEmpty(tutor.Expertise)
                ? tutor.Expertise.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                 .Select(e => e.Trim())
                                 .ToList()
                : new List<string>();

            return new TutorProfileViewModel
            {
                TutorId = tutor.TutorId,
                Name = tutor.Name + " " + tutor.Surname,
                Bio = tutor.Bio,
                AboutMe = tutor.AboutMe,
                ProfileImageUrl = string.IsNullOrEmpty(tutor.ProfileImageUrl)
                    ? Url.Content("~/images/default-profile.png")
                    : tutor.ProfileImageUrl,
                ExpertiseList = expertiseList,
                EducationList = !string.IsNullOrEmpty(tutor.Education)
                    ? JsonSerializer.Deserialize<List<EducationDTO>>(tutor.Education) ?? new List<EducationDTO>()
                    : new List<EducationDTO>()
            };
        }


        // -------------------------
        // Helper: Get current UserId
        // -------------------------
        private int? GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out int userId) ? userId : (int?)null;
        }

        // -------------------------
        // Helper: Redirect to login with optional error
        // -------------------------
        private IActionResult RedirectToLogin(string errorMessage = "User information missing. Please log in again.")
        {
            TempData["ErrorMessage"] = errorMessage;
            return RedirectToAction("Login", "Auth");
        }


        [HttpGet]
        [Authorize(Roles = "Tutor")]
        public async Task<IActionResult> MySessions()
        {
            try
            {
                // Get the logged-in tutor's ID
                var tutorId = await GetLoggedInTutorIdAsync();

                // Get all bookings for this tutor
                var bookings = await _apiService.GetTutorBookingsAsync(tutorId);

                ViewBag.PendingCount = bookings.Count(b => b.Status == "Pending");
                ViewBag.TutorId = tutorId;

                return View(bookings);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error loading sessions. Please try again.";
                return RedirectToAction("Dashboard");
            }
        }

        private async Task<int> GetLoggedInTutorIdAsync()
        {
            // Implement logic to get the logged-in tutor's ID
            // This might be from claims, session, or API call
            var tutorIdClaim = User.FindFirst("TutorId")?.Value;
            if (!string.IsNullOrEmpty(tutorIdClaim) && int.TryParse(tutorIdClaim, out int tutorId))
            {
                return tutorId;
            }

            // Fallback: Get tutor by user ID
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int userId))
            {
                var tutor = await _apiService.GetTutorByUserIdAsync(userId);
                if (tutor != null)
                {
                    return tutor.TutorId;
                }
            }

            throw new Exception("Tutor not found. Please ensure you're logged in as a tutor.");
        }
    }
}
