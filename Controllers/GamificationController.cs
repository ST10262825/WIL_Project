// Controllers/GamificationController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TutorConnectAPI.Data;
using TutorConnectAPI.DTOs;
using TutorConnectAPI.Models;
using TutorConnectAPI.Services;
using Microsoft.EntityFrameworkCore;

namespace TutorConnectAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize]
    public class GamificationController : ControllerBase
    {
        private readonly IGamificationService _gamificationService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<GamificationController> _logger;

        public GamificationController(
            IGamificationService gamificationService,
            ApplicationDbContext context,
            ILogger<GamificationController> logger)
        {
            _gamificationService = gamificationService;
            _context = context;
            _logger = logger;
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetUserProfile()
        {
            try
            {
                var userId = GetCurrentUserId();
                _logger.LogInformation("=== API: Getting profile for user {UserId} ===", userId);

                var profile = await _gamificationService.GetUserProfileAsync(userId);

                // Debug log the returned profile
                if (profile != null)
                {
                    _logger.LogInformation("=== API: Profile Details ===");
                    _logger.LogInformation("User ID: {UserId}", profile.UserId);
                    _logger.LogInformation("Achievements Count: {AchievementsCount}", profile.Achievements?.Count ?? 0);

                    if (profile.Achievements != null && profile.Achievements.Any())
                    {
                        foreach (var achievement in profile.Achievements.Take(5)) // Limit to 5 for logs
                        {
                            _logger.LogInformation("  - {AchievementName}: IsCompleted={IsCompleted}",
                                achievement.Name, achievement.IsCompleted);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("No achievements in profile DTO");
                    }
                }
                else
                {
                    _logger.LogWarning("=== API: Profile is NULL ===");
                }

                return Ok(profile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "=== API ERROR in GetUserProfile ===");
                return StatusCode(500, $"Error retrieving profile: {ex.Message}");
            }
        }

        [HttpPost("award-points")]
        public async Task<IActionResult> AwardPoints([FromBody] AwardPointsRequest request)
        {
            try
            {
                // Validate the request
                if (request == null)
                    return BadRequest("Request cannot be null");

                if (request.UserId <= 0)
                    return BadRequest("Invalid UserId");

                var response = await _gamificationService.AwardPointsAsync(
                    request.UserId,
                    request.ActivityType,
                    request.Points,
                    request.Description);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error awarding points for user {UserId}", request?.UserId);
                return StatusCode(500, $"Error awarding points: {ex.Message}");
            }
        }

        [HttpGet("achievements")]
        public async Task<IActionResult> GetAchievements()
        {
            try
            {
                var achievements = await _gamificationService.GetAvailableAchievementsAsync();
                return Ok(achievements);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving achievements");
                return StatusCode(500, $"Error retrieving achievements: {ex.Message}");
            }
        }

        [HttpPost("session-completed/{bookingId}")]
        public async Task<IActionResult> OnSessionCompleted(int bookingId)
        {
            try
            {
                var booking = await _context.Bookings
                    .Include(b => b.Student)
                    .Include(b => b.Tutor)
                    .FirstOrDefaultAsync(b => b.BookingId == bookingId);

                if (booking == null)
                    return NotFound("Booking not found");

                // Award points to student
                var studentResponse = await _gamificationService.AwardPointsAsync(
     booking.Student.UserId,
     "SessionCompleted",
     50,
     "Completed a tutoring session");

                // Force achievement check after session completion
                await _gamificationService.CheckAndAwardAchievementsAsync(booking.Student.UserId);

                // Award points to tutor
                var tutorResponse = await _gamificationService.AwardPointsAsync(
                    booking.Tutor.UserId,
                    "SessionCompleted",
                    50,
                    "Completed a tutoring session");

                return Ok(new { Student = studentResponse, Tutor = tutorResponse });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing session completion for booking {BookingId}", bookingId);
                return StatusCode(500, $"Error processing session completion: {ex.Message}");
            }
        }

        // ===== DEBUG ENDPOINTS =====

        [HttpGet("debug-achievements/{userId}")]
        public async Task<IActionResult> DebugAchievements(int userId)
        {
            try
            {
                var profile = await _context.GamificationProfiles
                    .Include(p => p.Achievements)
                    .ThenInclude(ua => ua.Achievement)
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                var allAchievements = await _context.Achievements.ToListAsync();

                // Test each achievement criteria
                var achievementTests = new List<object>();

                foreach (var achievement in allAchievements)
                {
                    var criteria = System.Text.Json.JsonSerializer.Deserialize<AchievementRequirement>(
                        achievement.Criteria,
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    var testResult = await TestAchievementCriteria(userId, achievement, profile, criteria);
                    achievementTests.Add(testResult);
                }

                return Ok(new
                {
                    Profile = new
                    {
                        UserId = profile?.UserId,
                        XP = profile?.ExperiencePoints,
                        Level = profile?.Level,
                        Streak = profile?.StreakCount,
                        SessionCount = await GetUserSessionCount(userId)
                    },
                    AchievementTests = achievementTests,
                    AllAchievementsCount = allAchievements.Count,
                    UserAchievementsCount = profile?.Achievements?.Count ?? 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DebugAchievements for user {UserId}", userId);
                return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        [HttpGet("emergency-debug/{userId}")]
        public async Task<IActionResult> EmergencyDebug(int userId)
        {
            var debugSteps = new List<object>();

            try
            {
                _logger.LogInformation("=== EMERGENCY DEBUG FOR USER {UserId} ===", userId);

                // Step 1: Check if user exists
                var user = await _context.Users.FindAsync(userId);
                debugSteps.Add(new { Step = "1 - User Check", UserExists = user != null, UserId = userId });

                // Step 2: Check GamificationProfile
                var profile = await _context.GamificationProfiles
                    .FirstOrDefaultAsync(p => p.UserId == userId);
                debugSteps.Add(new
                {
                    Step = "2 - Profile Check",
                    ProfileExists = profile != null,
                    ProfileId = profile?.GamificationProfileId,
                    ProfileUserId = profile?.UserId
                });

                // Step 3: Check UserAchievements DIRECTLY (no includes)
                var userAchievementsCount = await _context.UserAchievements
                    .CountAsync(ua => ua.UserId == userId);
                debugSteps.Add(new
                {
                    Step = "3 - Direct UserAchievements Count",
                    Count = userAchievementsCount
                });

                // Step 4: Check UserAchievements with Achievement includes
                var userAchievementsWithIncludes = await _context.UserAchievements
                    .Where(ua => ua.UserId == userId)
                    .Include(ua => ua.Achievement)
                    .Take(5)
                    .ToListAsync();
                debugSteps.Add(new
                {
                    Step = "4 - UserAchievements with Includes",
                    Count = userAchievementsWithIncludes.Count,
                    Sample = userAchievementsWithIncludes.Select(ua => new {
                        UserAchievementId = ua.UserAchievementId,
                        UserId = ua.UserId,
                        AchievementId = ua.AchievementId,
                        AchievementName = ua.Achievement?.Name,
                        Progress = ua.Progress
                    })
                });

                // Step 5: Test Service Method Directly
                var serviceResult = await _gamificationService.GetUserProfileAsync(userId);
                debugSteps.Add(new
                {
                    Step = "5 - Service Method Result",
                    ServiceAchievementsCount = serviceResult?.Achievements?.Count ?? 0,
                    ServiceProfileExists = serviceResult != null
                });

                // Step 6: Check if there's a data mismatch
                debugSteps.Add(new
                {
                    Step = "6 - Data Analysis",
                    Issue = userAchievementsCount > 0 && (serviceResult?.Achievements?.Count ?? 0) == 0
                        ? "DATA IS BEING LOST IN SERVICE METHOD"
                        : "Consistent zero achievements",
                    DirectCount = userAchievementsCount,
                    ServiceCount = serviceResult?.Achievements?.Count ?? 0
                });

                return Ok(new
                {
                    UserId = userId,
                    DebugSteps = debugSteps,
                    Summary = new
                    {
                        TotalUserAchievementsInDb = userAchievementsCount,
                        AchievementsInServiceResult = serviceResult?.Achievements?.Count ?? 0,
                        ProblemIdentified = userAchievementsCount > 0 && (serviceResult?.Achievements?.Count ?? 0) == 0
                    }
                });
            }
            catch (Exception ex)
            {
                debugSteps.Add(new { Step = "ERROR", Exception = ex.Message, StackTrace = ex.StackTrace });
                _logger.LogError(ex, "Emergency debug failed for user {UserId}", userId);
                return StatusCode(500, new { debugSteps });
            }
        }

        [HttpGet("manual-profile/{userId}")]
        public async Task<IActionResult> GetManualProfile(int userId)
        {
            try
            {
                // Get profile
                var profile = await _context.GamificationProfiles
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (profile == null)
                    return NotFound("Profile not found");

                // Manually load achievements - COMPLETELY bypass EF navigation
                var userAchievements = await _context.UserAchievements
                    .Where(ua => ua.UserId == userId)
                    .Include(ua => ua.Achievement)
                    .ToListAsync();

                _logger.LogInformation("Manual load found {Count} achievements for user {UserId}",
                    userAchievements.Count, userId);

                // Calculate level progression
                var pointsForNextLevel = CalculatePointsForLevel(profile.Level + 1);
                var pointsForCurrentLevel = CalculatePointsForLevel(profile.Level);
                var levelProgress = pointsForNextLevel > pointsForCurrentLevel
                    ? (decimal)(profile.ExperiencePoints - pointsForCurrentLevel) / (pointsForNextLevel - pointsForCurrentLevel)
                    : 0;

                // Build DTO manually
                var profileDto = new GamificationProfileDTO
                {
                    UserId = profile.UserId,
                    ExperiencePoints = profile.ExperiencePoints,
                    Level = profile.Level,
                    CurrentRank = GetRankForLevel(profile.Level),
                    StreakCount = profile.StreakCount,
                    PointsToNextLevel = Math.Max(0, pointsForNextLevel - profile.ExperiencePoints),
                    LevelProgress = levelProgress,
                    Achievements = userAchievements.Select(ua => new UserAchievementDTO
                    {
                        Name = ua.Achievement?.Name ?? "Unknown",
                        Description = ua.Achievement?.Description ?? "",
                        IconUrl = ua.Achievement?.IconUrl ?? "",
                        EarnedAt = ua.EarnedAt,
                        Progress = ua.Progress,
                        TotalRequired = 100,
                        IsCompleted = ua.Progress >= 100
                    }).ToList()
                };

                return Ok(new
                {
                    Success = true,
                    Profile = profileDto,
                    ManualAchievementsCount = userAchievements.Count,
                    UsingNavigationProperty = false
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in manual profile for user {UserId}", userId);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // ===== PRIVATE METHODS =====

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int userId))
                return userId;
            throw new UnauthorizedAccessException("User ID not found in claims");
        }

        private async Task<object> TestAchievementCriteria(int userId, Achievement achievement, GamificationProfile profile, AchievementRequirement criteria)
        {
            try
            {
                bool meetsCriteria = false;
                string details = "";

                switch (achievement.Type)
                {
                    case "Attendance":
                        switch (criteria.CriteriaType)
                        {
                            case "session_count":
                                var sessionCount = await GetUserSessionCount(userId);
                                meetsCriteria = sessionCount >= criteria.RequiredCount;
                                details = $"Sessions: {sessionCount}, Required: {criteria.RequiredCount}";
                                break;
                            case "login_streak":
                                meetsCriteria = (profile?.StreakCount ?? 0) >= criteria.RequiredCount;
                                details = $"Streak: {profile?.StreakCount}, Required: {criteria.RequiredCount}";
                                break;
                            case "account_created":
                                meetsCriteria = (profile?.ExperiencePoints ?? 0) > 0;
                                details = $"Has XP: {(profile?.ExperiencePoints ?? 0) > 0}";
                                break;
                        }
                        break;
                    case "Progress":
                        switch (criteria.CriteriaType)
                        {
                            case "reach_level":
                                meetsCriteria = (profile?.Level ?? 0) >= criteria.RequiredCount;
                                details = $"Level: {profile?.Level}, Required: {criteria.RequiredCount}";
                                break;
                        }
                        break;
                }

                return new
                {
                    AchievementId = achievement.AchievementId,
                    Name = achievement.Name,
                    Type = achievement.Type,
                    CriteriaType = criteria.CriteriaType,
                    Required = criteria.RequiredCount,
                    MeetsCriteria = meetsCriteria,
                    Details = details,
                    AlreadyEarned = profile?.Achievements?.Any(ua => ua.AchievementId == achievement.AchievementId && ua.Progress >= 100) ?? false
                };
            }
            catch (Exception ex)
            {
                return new
                {
                    AchievementId = achievement.AchievementId,
                    Name = achievement.Name,
                    Error = ex.Message
                };
            }
        }

        private async Task<int> GetUserSessionCount(int userId)
        {
            var user = await _context.Users
                .Include(u => u.Student)
                .Include(u => u.Tutor)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null) return 0;

            if (user.Role == "Student" && user.Student != null)
            {
                return await _context.Bookings
                    .CountAsync(b => b.StudentId == user.Student.StudentId && b.Status == "Completed");
            }
            else if (user.Role == "Tutor" && user.Tutor != null)
            {
                return await _context.Bookings
                    .CountAsync(b => b.TutorId == user.Tutor.TutorId && b.Status == "Completed");
            }

            return 0;
        }

        private int CalculatePointsForLevel(int level)
        {
            return 100 * (level - 1) * (level - 1);
        }

        private string GetRankForLevel(int level)
        {
            return level switch
            {
                < 5 => "Beginner",
                < 10 => "Intermediate",
                < 15 => "Advanced",
                < 20 => "Expert",
                _ => "Master"
            };
        }

        [HttpPost("force-create-achievements/{userId}")]
        public async Task<IActionResult> ForceCreateAchievements(int userId)
        {
            try
            {
                _logger.LogInformation("=== FORCE CREATING ACHIEVEMENTS FOR USER {UserId} ===", userId);

                // Get all available achievements
                var allAchievements = await _context.Achievements.ToListAsync();
                var createdCount = 0;

                // Check which achievements the user should have based on their profile
                var profile = await _context.GamificationProfiles
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (profile == null)
                {
                    return NotFound("Profile not found");
                }

                var sessionCount = await GetUserSessionCount(userId);

                foreach (var achievement in allAchievements)
                {
                    var criteria = System.Text.Json.JsonSerializer.Deserialize<AchievementRequirement>(
                        achievement.Criteria,
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (criteria == null) continue;

                    bool shouldHaveAchievement = false;

                    // Check if user meets criteria for this achievement
                    switch (achievement.Type)
                    {
                        case "Attendance":
                            switch (criteria.CriteriaType)
                            {
                                case "account_created":
                                    shouldHaveAchievement = profile.ExperiencePoints > 0;
                                    break;
                                case "session_count":
                                    shouldHaveAchievement = sessionCount >= criteria.RequiredCount;
                                    break;
                                case "login_streak":
                                    shouldHaveAchievement = profile.StreakCount >= criteria.RequiredCount;
                                    break;
                            }
                            break;
                        case "Progress":
                            switch (criteria.CriteriaType)
                            {
                                case "reach_level":
                                    shouldHaveAchievement = profile.Level >= criteria.RequiredCount;
                                    break;
                            }
                            break;
                    }

                    if (shouldHaveAchievement)
                    {
                        var existing = await _context.UserAchievements
                            .FirstOrDefaultAsync(ua => ua.UserId == userId && ua.AchievementId == achievement.AchievementId);

                        if (existing == null)
                        {
                            var userAchievement = new UserAchievement
                            {
                                UserId = userId,
                                AchievementId = achievement.AchievementId,
                                EarnedAt = DateTime.UtcNow.AddDays(-createdCount),
                                Progress = 100,
                                GamificationProfileId = profile.GamificationProfileId // ADD THIS LINE
                            };
                            _context.UserAchievements.Add(userAchievement);
                            createdCount++;
                            _logger.LogInformation("Creating achievement: {AchievementName} with ProfileId {ProfileId}",
                                achievement.Name, profile.GamificationProfileId);
                        }
                    }
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Created {Count} achievements for user {UserId}", createdCount, userId);

                // Verify the creation
                var finalCount = await _context.UserAchievements
                    .CountAsync(ua => ua.UserId == userId);

                return Ok(new
                {
                    Success = true,
                    AchievementsCreated = createdCount,
                    TotalAchievementsNow = finalCount,
                    Message = $"Created {createdCount} achievements for user {userId}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error force creating achievements for user {UserId}", userId);
                return StatusCode(500, new { error = ex.Message });
            }
        }


        [HttpGet("xp-breakdown")]
        public async Task<IActionResult> GetXPBreakdown()
        {
            try
            {
                var userId = GetCurrentUserId();
                var breakdown = await _gamificationService.GetXPBreakdownAsync(userId);
                return Ok(breakdown);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting XP breakdown");
                return StatusCode(500, "Error retrieving XP breakdown");
            }
        }

        [HttpGet("recent-xp-activity")]
        public async Task<IActionResult> GetRecentXPActivity()
        {
            try
            {
                var userId = GetCurrentUserId();
                var activities = await _gamificationService.GetRecentXPActivityAsync(userId);
                return Ok(activities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent XP activity");
                return StatusCode(500, "Error retrieving recent XP activity");
            }
        }

    }
}