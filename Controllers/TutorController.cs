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

            // Get tutor reviews
            var reviews = await _apiService.GetTutorReviewsAsync(tutorId);

            // Get tutor availability for the current week
            var availability = await _apiService.GetTutorAvailabilityAsync(tutorId, DateTime.Today);


            var model = MapTutorToProfileViewModel(tutor, reviews,availability);
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
        // Browse Tutors with Filtering
        // -------------------------
        [HttpGet]
        public async Task<IActionResult> Browse(string searchString = "", string subject = "")
        {
            try
            {
                var tutors = await _apiService.GetAllTutorsAsync();

                // Apply filters
                var filteredTutors = tutors.AsQueryable();

                // Filter by search string (tutor name)
                if (!string.IsNullOrEmpty(searchString))
                {
                    searchString = searchString.Trim().ToLower();
                    filteredTutors = filteredTutors.Where(t =>
                        (t.FullName).ToLower().Contains(searchString) ||
                        t.FullName.ToLower().Contains(searchString));
                }

                // Filter by subject
                if (!string.IsNullOrEmpty(subject))
                {
                    filteredTutors = filteredTutors.Where(t =>
                        t.Subjects != null && t.Subjects.Any(s =>
                            s.Equals(subject, StringComparison.OrdinalIgnoreCase)));
                }

                // Get unique subjects for the filter dropdown
                var allSubjects = tutors
                    .Where(t => t.Subjects != null)
                    .SelectMany(t => t.Subjects)
                    .Distinct()
                    .OrderBy(s => s)
                    .ToList();

                ViewBag.Subjects = allSubjects;

                // Pass search parameters to view for persistence
                ViewBag.SearchString = searchString;
                ViewBag.SelectedSubject = subject;

                return View(filteredTutors.ToList());
            }
            catch (Exception ex)
            {
                // Handle error
                Console.WriteLine($"Error in Browse: {ex.Message}");
                TempData["ErrorMessage"] = "Error loading tutors. Please try again.";
                return View(new List<TutorDTO>());
            }
        }

        // -------------------------
        // Helper: Map TutorDTO to TutorProfileViewModel
        // -------------------------
        private TutorProfileViewModel MapTutorToProfileViewModel(TutorDTO tutor, List<ReviewDTO> reviews = null, List<TimeSlotDTO> availability = null)
        {
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
                    : new List<EducationDTO>(),

                // Rating properties
            AverageRating = tutor.AverageRating,
            TotalReviews = tutor.TotalReviews,
            RatingCount1 = tutor.RatingCount1,
            RatingCount2 = tutor.RatingCount2,
            RatingCount3 = tutor.RatingCount3,
            RatingCount4 = tutor.RatingCount4,
            RatingCount5 = tutor.RatingCount5,
                Reviews = reviews ?? new List<ReviewDTO>(),
                 Availability = availability




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

        // In your TutorController
        [HttpGet("Tutor/Availability/{tutorId}")]
        public async Task<IActionResult> GetTutorAvailabilityWeb(int tutorId, [FromQuery] string date)
        {
            Console.WriteLine($"WebApp Availability Called: tutorId={tutorId}, date={date}");

            if (!DateTime.TryParse(date, out var startDate))
            {
                startDate = DateTime.Today;
            }

            // Get availability for the entire week
            var availability = new List<TimeSlotDTO>();

            for (int i = 0; i < 7; i++)
            {
                var currentDate = startDate.AddDays(i);
                var dayAvailability = await _apiService.GetTutorAvailabilityAsync(tutorId, currentDate);
                availability.AddRange(dayAvailability);
            }

            Console.WriteLine($"Returning {availability.Count} time slots for week starting {startDate:yyyy-MM-dd}");
            return Ok(availability);
        }
    }
}
