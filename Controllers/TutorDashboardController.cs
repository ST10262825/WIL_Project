using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using TutorConnect.WebApp.Models;
using TutorConnect.WebApp.Services;

namespace TutorConnect.WebApp.Controllers
{
    public class TutorDashboardController : Controller
    {
        private readonly ApiService _apiService;

        public TutorDashboardController(ApiService apiService)
        {
            _apiService = apiService;
        }

        public async Task<IActionResult> Index()
        {
            // Step 1: Get the current user's ID from Claims
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                return Unauthorized(); // Or redirect to login
            }

            // Step 2: Get the tutor associated with this user
            var tutor = await _apiService.GetTutorByUserIdAsync(userId);
            if (tutor == null)
            {
                return NotFound("Tutor not found");
            }

            int tutorId = tutor.Id;

            // Step 3: Use tutorId to load dashboard data
            var sessions = await _apiService.GetTutorSessionsAsync(tutorId);
            var summary = await _apiService.GetTutorSessionSummaryAsync(tutorId);

            var viewModel = new TutorDashboardViewModel
            {
                Sessions = sessions,
                Summary = summary
            };

            return View(viewModel);
        }




        [HttpPost]
        public async Task<IActionResult> UpdateSessionStatus(int sessionId, bool approve, string? rejectionReason)
        {
            HttpResponseMessage response;

            if (approve)
            {
                response = await _apiService.ApproveSessionAsync(sessionId);
            }
            else
            {
                response = await _apiService.RejectSessionAsync(sessionId, rejectionReason ?? "");
            }

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Failed to update session status.";
                // optionally return to view with error message, not redirect
            }

            return RedirectToAction("Index");
        }



    }
}