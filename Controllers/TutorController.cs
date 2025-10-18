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

            // ADD COURSE INFO
            ViewBag.CourseName = tutor.CourseName ?? "Not assigned to course";

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

            ViewBag.CourseName = tutor.CourseName ?? "Not assigned to course";

            return View(model);
        }

        // -------------------------
        // MyProfile (POST)
        // -------------------------
        [HttpPost]
        public async Task<IActionResult> MyProfile(TutorProfileViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["ErrorMessage"] = "Please correct the validation errors.";
                    return View(model);
                }

                var expertiseCsv = model.ExpertiseList != null ? string.Join(", ", model.ExpertiseList) : "";
                var educationJson = model.EducationList != null
                    ? JsonSerializer.Serialize(model.EducationList)
                    : "[]";

                Console.WriteLine($"Sending to API - Expertise: {expertiseCsv}");
                Console.WriteLine($"Sending to API - Education JSON: {educationJson}");

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
                    Console.WriteLine($"API Error: {ErrorMessage}");
                    return View(model);
                }

                TempData["SuccessMessage"] = "Profile updated successfully!";
                if (!string.IsNullOrEmpty(ProfileImageUrl))
                    ViewBag.TutorProfileImageUrl = $"{ProfileImageUrl}?v={DateTime.UtcNow.Ticks}";

                return RedirectToAction("Dashboard");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in MyProfile POST: {ex.Message}");
                Console.WriteLine($"Stack: {ex.StackTrace}");
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
                return View(model);
            }
        }

        // -------------------------
        // Profile view by TutorId
        // -------------------------
        [HttpGet("/Tutor/Profile/{tutorId}")]
        public async Task<IActionResult> Profile(int tutorId)
        {
            var tutor = await _apiService.GetTutorByIdAsync(tutorId);
            if (tutor == null) return NotFound();

            // ADD THIS: Check if student can access this tutor (same course)
            var student = await GetCurrentStudentAsync();
            if (student != null && student.CourseId != tutor.CourseId)
            {
                TempData["ErrorMessage"] = "You can only view tutors in your course.";
                return RedirectToAction("Browse");
            }

            // Get tutor reviews
            var reviews = await _apiService.GetTutorReviewsAsync(tutorId);

            // Get tutor availability for the current week
            var availability = await _apiService.GetTutorAvailabilityAsync(tutorId, DateTime.Today);

            var model = MapTutorToProfileViewModel(tutor, reviews, availability);
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
                var student = await GetCurrentStudentAsync();
                List<BrowseTutorDTO> tutors;

                if (student != null)
                {
                    // USE THE NEW API METHOD - More secure and efficient
                    tutors = await _apiService.GetTutorsForStudentAsync(student.StudentId);
                }
                else
                {
                    // Fallback to old method (for admin viewing or edge cases)
                    tutors = await _apiService.GetAllTutorsAsync();
                }

                // Apply additional filters (search, subject) - this is fine client-side
                var filteredTutors = tutors.AsQueryable();

                // Filter by search string (tutor name)
                if (!string.IsNullOrEmpty(searchString))
                {
                    searchString = searchString.Trim().ToLower();
                    filteredTutors = filteredTutors.Where(t =>
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
                ViewBag.StudentCourse = student?.CourseName ?? "All Courses";
                ViewBag.SearchString = searchString;
                ViewBag.SelectedSubject = subject;

                return View(filteredTutors.ToList());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Browse: {ex.Message}");
                TempData["ErrorMessage"] = "Error loading tutors. Please try again.";
                return View(new List<BrowseTutorDTO>());
            }
        }

        // -------------------------
        // Helper: Get current Student with Course info
        // -------------------------
        private async Task<StudentDTO> GetCurrentStudentAsync()
        {
            var userId = GetUserId();
            if (userId == null) return null;

            try
            {
                var student = await _apiService.GetStudentByUserIdAsync(userId.Value);
                return student;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting current student: {ex.Message}");
                return null;
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

                // ADD COURSE INFO
                CourseName = tutor.CourseName, // Add this property to TutorProfileViewModel
                CourseId = tutor.CourseId,     // Add this property to TutorProfileViewModel

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

                // ADD: Get tutor info for course context
                var userId = GetUserId();
                var tutor = await _apiService.GetTutorByUserIdAsync(userId.Value);

                // Get all bookings for this tutor
                var bookings = await _apiService.GetTutorBookingsAsync(tutorId);

                ViewBag.PendingCount = bookings.Count(b => b.Status == "Pending");
                ViewBag.TutorId = tutorId;

                // ADD COURSE INFO
                ViewBag.CourseName = tutor?.CourseName ?? "Not assigned";

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

            // ADD THIS: Check if student can access this tutor (same course)
            var student = await GetCurrentStudentAsync();
            if (student != null)
            {
                var tutor = await _apiService.GetTutorByIdAsync(tutorId);
                if (tutor != null && student.CourseId != tutor.CourseId)
                {
                    return Unauthorized("You can only view availability for tutors in your course.");
                }
            }

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


        // In your WebApp TutorController
        [HttpGet]
        public async Task<IActionResult> Materials()
        {
            var userId = GetUserId();
            if (userId == null) return RedirectToLogin();

            var tutor = await _apiService.GetTutorByUserIdAsync(userId.Value);
            if (tutor == null) return NotFound();

            // Get materials overview from API - THIS SHOULD GET FRESH DATA
            var materialsOverview = await _apiService.GetTutorMaterialsOverviewAsync(tutor.TutorId);

            ViewBag.TutorId = tutor.TutorId;
            ViewBag.TutorName = $"{tutor.Name} {tutor.Surname}";

            ViewBag.CourseName = tutor.CourseName ?? "Not assigned to course";

            return View(materialsOverview);
        }

        // In your WebApp TutorController - FIX THESE METHODS
        [HttpPost]
        public async Task<IActionResult> CreateFolder(int tutorId, string name, string description, int? parentFolderId)
        {
            try
            {
                Console.WriteLine($"[WEBAPP DEBUG] CreateFolder START");
                Console.WriteLine($"[WEBAPP DEBUG] Parameters - TutorId: {tutorId}, Name: {name}, Description: {description}, ParentFolderId: {parentFolderId}");

                // Validate required fields
                if (string.IsNullOrEmpty(name))
                {
                    TempData["ErrorMessage"] = "Folder name is required";
                    Console.WriteLine($"[WEBAPP DEBUG] Validation failed - Name is empty");
                    return RedirectToAction("Materials");
                }

                Console.WriteLine($"[WEBAPP DEBUG] Calling API Service...");
                var result = await _apiService.CreateFolderAsync(tutorId, name, description, parentFolderId);

                Console.WriteLine($"[WEBAPP DEBUG] API Service returned - IsSuccess: {result.IsSuccess}, Error: {result.ErrorMessage}");

                if (result.IsSuccess)
                {
                    TempData["SuccessMessage"] = "Folder created successfully!";
                    Console.WriteLine($"[WEBAPP DEBUG] Folder created successfully");
                }
                else
                {
                    TempData["ErrorMessage"] = $"Failed to create folder: {result.ErrorMessage}";
                    Console.WriteLine($"[WEBAPP DEBUG] Folder creation failed");
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error creating folder: {ex.Message}";
                Console.WriteLine($"[WEBAPP DEBUG] Exception: {ex}");
            }

            Console.WriteLine($"[WEBAPP DEBUG] CreateFolder END - Redirecting to Materials");
            return RedirectToAction("Materials");
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadMaterial(
    int tutorId,
    string title,
    string description,
    int? folderId,
    string isPublic,  // Change from bool to string
    IFormFile file)
        {
            try
            {
                Console.WriteLine($"[WEBAPP UPLOAD] UploadMaterial START");
                Console.WriteLine($"[WEBAPP UPLOAD] TutorId: {tutorId}, Title: {title}, FolderId: {folderId}, IsPublic: '{isPublic}'");
                Console.WriteLine($"[WEBAPP UPLOAD] File: {file?.FileName}, Size: {file?.Length}");

                // Convert string to bool
                bool isPublicBool = isPublic == "true";
                Console.WriteLine($"[WEBAPP UPLOAD] IsPublic converted: {isPublicBool}");

                if (file == null || file.Length == 0)
                {
                    TempData["ErrorMessage"] = "Please select a file to upload.";
                    Console.WriteLine($"[WEBAPP UPLOAD] No file selected");
                    return RedirectToAction("Materials");
                }

                Console.WriteLine($"[WEBAPP UPLOAD] Calling API Service...");
                var result = await _apiService.UploadMaterialAsync(tutorId, title, description, folderId, isPublicBool, file);

                if (result.IsSuccess)
                {
                    TempData["SuccessMessage"] = $"Material '{title}' uploaded successfully!";
                    Console.WriteLine($"[WEBAPP UPLOAD] SUCCESS - Material ID: {result.Material?.LearningMaterialId}");
                }
                else
                {
                    TempData["ErrorMessage"] = $"Failed to upload material: {result.ErrorMessage}";
                    Console.WriteLine($"[WEBAPP UPLOAD] FAILED: {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error uploading material: {ex.Message}";
                Console.WriteLine($"[WEBAPP UPLOAD] EXCEPTION: {ex}");
            }

            return RedirectToAction("Materials");
        }


        [HttpPost("DeleteMaterial")]
        public async Task<IActionResult> DeleteMaterial([FromForm] int materialId)
        {
            try
            {
                Console.WriteLine($"[WEBAPP] DeleteMaterial called - MaterialId: {materialId}");

                var success = await _apiService.DeleteMaterialAsync(materialId);
                if (success)
                {
                    TempData["SuccessMessage"] = "Material deleted successfully!";
                    Console.WriteLine($"[WEBAPP] Material deleted successfully");
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to delete material.";
                    Console.WriteLine($"[WEBAPP] Material deletion failed");
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting material: {ex.Message}";
                Console.WriteLine($"[WEBAPP] Exception in DeleteMaterial: {ex}");
            }

            return RedirectToAction("Materials");
        }




        
        [HttpGet("materials/folder/{folderId}")]
        public async Task<IActionResult> FolderContents(int folderId)
        {
            var userId = GetUserId();
            if (userId == null) return RedirectToLogin();

            var tutor = await _apiService.GetTutorByUserIdAsync(userId.Value);
            if (tutor == null) return NotFound();

            var folder = await _apiService.GetFolderContentsAsync(tutor.TutorId, folderId);
            if (folder == null) return NotFound();

            ViewBag.TutorId = tutor.TutorId;
            ViewBag.TutorName = $"{tutor.Name} {tutor.Surname}";

            return View("FolderContents", folder);
        }


        [HttpPost("materials/delete-folder")]
        public async Task<IActionResult> DeleteFolder(int folderId)
        {
            try
            {
                Console.WriteLine($"[WEBAPP] DeleteFolder called - FolderId: {folderId}");

                // First check if folder has any materials
                var userId = GetUserId();
                var tutor = await _apiService.GetTutorByUserIdAsync(userId.Value);

                if (tutor == null)
                {
                    TempData["ErrorMessage"] = "Tutor not found.";
                    return RedirectToAction("Materials");
                }

                // Call API to delete folder
                var success = await _apiService.DeleteFolderAsync(folderId);

                if (success)
                {
                    TempData["SuccessMessage"] = "Folder deleted successfully!";
                    Console.WriteLine($"[WEBAPP] Folder deleted successfully");
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to delete folder. It may contain materials or subfolders.";
                    Console.WriteLine($"[WEBAPP] Folder deletion failed");
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting folder: {ex.Message}";
                Console.WriteLine($"[WEBAPP] Exception in DeleteFolder: {ex}");
            }

            return RedirectToAction("Materials");
        }

    }
}
