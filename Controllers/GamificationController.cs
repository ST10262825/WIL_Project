// Controllers/GamificationController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TutorConnect.WebApp.Models;
using TutorConnect.WebApp.Services;

namespace TutorConnect.WebApp.Controllers
{
    [Authorize]
    public class GamificationController : Controller
    {
        private readonly ApiService _apiService;

        public GamificationController(ApiService apiService)
        {
            _apiService = apiService;
        }

        // In your GamificationController
        public async Task<IActionResult> Profile()
        {
            try
            {
                //var userId = GetCurrentUserId();
                var profile = await _apiService.GetGamificationProfileAsync();
                var achievements = await _apiService.GetAchievementsAsync();

                if (profile == null)
                {
                    TempData["Error"] = "Unable to load your gamification profile";
                    return RedirectToAction("Index", "Home");
                }

                // NEW: Load XP breakdown and recent activity
                var xpBreakdown = await _apiService.GetXPBreakdownAsync();
                var recentXPActivity = await _apiService.GetRecentXPActivityAsync();

                var viewModel = new GamificationProfileViewModel
                {
                    Profile = profile,
                    AllAchievements = achievements,
                    RecentActivity = await GetRecentActivityAsync(),
                    XPBreakdown = xpBreakdown ?? new XPBreakdownDTO(), // Fallback if null
                    RecentXPActivity = recentXPActivity ?? new List<XPActivityDTO>() // Fallback if null
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading profile: {ex.Message}";
                return RedirectToAction("Index", "Home");
            }
        }



        // Achievement gallery
        public async Task<IActionResult> Achievements()
        {
            try
            {
                var profile = await _apiService.GetGamificationProfileAsync();
                var achievements = await _apiService.GetAchievementsAsync();

                var viewModel = new AchievementsViewModel
                {
                    Profile = profile,
                    Achievements = achievements,
                    EarnedCount = profile?.Achievements.Count(a => a.IsCompleted) ?? 0,
                    TotalCount = achievements?.Count ?? 0
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading achievements: {ex.Message}";
                return RedirectToAction("Profile");
            }
        }

        // Leaderboard (optional)
        public async Task<IActionResult> Leaderboard()
        {
            // You can implement this later if you want competitive elements
            return View();
        }

        private async Task<List<ActivityItem>> GetRecentActivityAsync()
        {
            // This would track recent point earnings, achievements unlocked, etc.
            // For now, return empty list - you can implement this later
            return new List<ActivityItem>();
        }


        
    }
}