using Microsoft.EntityFrameworkCore;
using System.Globalization;
using TutorConnectAPI.Models;
using TutorConnectAPI.Data;
using Microsoft.Extensions.Logging;
using TutorConnectAPI.DTOs;

namespace TutorConnectAPI.Services
{
    public interface IGamificationService
    {
        Task<ActivityResponse> AwardPointsAsync(int userId, string activityType, int points, string description = null);
        Task CheckAndAwardAchievementsAsync(int userId);
        Task UpdateStreakAsync(int userId);
        Task<GamificationProfileDTO> GetUserProfileAsync(int userId);
        Task<List<Achievement>> GetAvailableAchievementsAsync();
        Task<bool> TestAchievementCriteria(int userId, int achievementId);
        Task<XPBreakdownDTO> GetXPBreakdownAsync(int userId);
        Task<List<XPActivityDTO>> GetRecentXPActivityAsync(int userId);
    }

    // FIXED: Renamed to avoid ambiguity
    public class AchievementRequirement
    {
        public string CriteriaType { get; set; } = string.Empty;
        public int RequiredCount { get; set; }
        public string? AdditionalData { get; set; }
    }

    public class GamificationService : IGamificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<GamificationService> _logger;

        public GamificationService(ApplicationDbContext context, ILogger<GamificationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ActivityResponse> AwardPointsAsync(int userId, string activityType, int points, string description = null)
        {
            var response = new ActivityResponse();

            // Get or create user profile
            var profile = await _context.GamificationProfiles
                .Include(p => p.Achievements)
                .ThenInclude(ua => ua.Achievement)
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile == null)
            {
                profile = new GamificationProfile { UserId = userId };
                _context.GamificationProfiles.Add(profile);
            }

            // Award points
            profile.ExperiencePoints += points;
            profile.LastActivityDate = DateTime.UtcNow;

            // Check for level up
            var oldLevel = profile.Level;
            profile.Level = CalculateLevel(profile.ExperiencePoints);

            if (profile.Level > oldLevel)
            {
                response.NewLevel = profile.Level;
                response.Message = $"Level up! You reached level {profile.Level}";
            }

            // Update streak
            await UpdateStreakAsync(userId);

            await _context.SaveChangesAsync();

            // Check for new achievements
            await CheckAndAwardAchievementsAsync(userId);

            response.Success = true;
            response.PointsAwarded = points;

            _logger.LogInformation("Awarded {Points} points to user {UserId} for {ActivityType}", points, userId, activityType);

            return response;
        }

        public async Task CheckAndAwardAchievementsAsync(int userId)
        {
            var profile = await _context.GamificationProfiles
                .Include(p => p.Achievements)
                .ThenInclude(ua => ua.Achievement)
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile == null) return;

            var allAchievements = await _context.Achievements.ToListAsync();
            var unlockedAchievements = new List<string>();

            foreach (var achievement in allAchievements)
            {
                // Skip if already earned
                if (profile.Achievements.Any(ua => ua.AchievementId == achievement.AchievementId && ua.Progress >= 100))
                    continue;

                var isUnlocked = await CheckAchievementCriteriaAsync(userId, achievement, profile);

                if (isUnlocked)
                {
                    var userAchievement = profile.Achievements.FirstOrDefault(ua => ua.AchievementId == achievement.AchievementId);
                    if (userAchievement == null)
                    {
                        userAchievement = new UserAchievement
                        {
                            UserId = userId,
                            AchievementId = achievement.AchievementId,
                            EarnedAt = DateTime.UtcNow,
                            Progress = 100,
                            GamificationProfileId = profile.GamificationProfileId // Added this line
                        };
                        _context.UserAchievements.Add(userAchievement);
                    }
                    else
                    {
                        userAchievement.Progress = 100;
                        userAchievement.EarnedAt = DateTime.UtcNow;
                        userAchievement.GamificationProfileId = profile.GamificationProfileId; // Added this line
                    }

                    // Award achievement points
                    await AwardPointsAsync(userId, "AchievementEarned", achievement.PointsReward, $"Achievement: {achievement.Name}");

                    unlockedAchievements.Add(achievement.Name);

                    _logger.LogInformation("User {UserId} unlocked achievement: {AchievementName}", userId, achievement.Name);
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task UpdateStreakAsync(int userId)
        {
            var profile = await _context.GamificationProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
            if (profile == null) return;

            var today = DateTime.UtcNow.Date;
            var lastActivity = profile.LastActivityDate.Date;

            // If already updated today, do nothing
            if (lastActivity == today) return;

            if (lastActivity == today.AddDays(-1))
            {
                // Consecutive day
                profile.StreakCount++;
                _logger.LogInformation("User {UserId} streak incremented to {Streak}", userId, profile.StreakCount);
            }
            else if (lastActivity < today.AddDays(-1))
            {
                // Streak broken (missed one or more days)
                profile.StreakCount = 1;
                _logger.LogInformation("User {UserId} streak reset to 1", userId);
            }

            profile.LastActivityDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task<GamificationProfileDTO> GetUserProfileAsync(int userId)
        {
            _logger.LogInformation("=== GetUserProfileAsync for user {UserId} ===", userId);

            try
            {
                // STEP 1: Get profile (without achievements first)
                var profile = await _context.GamificationProfiles
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (profile == null)
                {
                    _logger.LogInformation("Creating new gamification profile for user {UserId}", userId);

                    profile = new GamificationProfile
                    {
                        UserId = userId,
                        Level = 1,
                        CurrentRank = "Beginner",
                        ExperiencePoints = 0,
                        StreakCount = 0,
                        LastActivityDate = DateTime.UtcNow
                    };

                    _context.GamificationProfiles.Add(profile);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Created profile ID: {ProfileId} for user {UserId}",
                        profile.GamificationProfileId, userId);

                    // NEW: Award initial "Getting Started" achievement automatically to new users
                    var gettingStartedAchievement = await _context.Achievements
                        .FirstOrDefaultAsync(a => a.Name == "Getting Started");

                    if (gettingStartedAchievement != null)
                    {
                        var initialAchievement = new UserAchievement
                        {
                            UserId = userId,
                            AchievementId = gettingStartedAchievement.AchievementId,
                            EarnedAt = DateTime.UtcNow,
                            Progress = 100,
                            GamificationProfileId = profile.GamificationProfileId // THIS IS KEY!
                        };
                        _context.UserAchievements.Add(initialAchievement);
                        await _context.SaveChangesAsync();

                        _logger.LogInformation("Awarded 'Getting Started' achievement to new user {UserId}", userId);
                    }
                    else
                    {
                        _logger.LogWarning("'Getting Started' achievement not found in database");
                    }
                }

                _logger.LogInformation("Profile loaded: ID={ProfileId}, UserId={UserId}, XP={XP}, Level={Level}",
                    profile.GamificationProfileId, profile.UserId, profile.ExperiencePoints, profile.Level);

                // STEP 2: Load achievements using proper EF navigation (now that relationships are fixed)
                _logger.LogInformation("Loading achievements for user {UserId}...", userId);

                List<UserAchievementDTO> userAchievements;

                // Try using EF navigation first (should work now with fixed relationships)
                var profileWithAchievements = await _context.GamificationProfiles
                    .Include(p => p.Achievements)
                    .ThenInclude(ua => ua.Achievement)
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (profileWithAchievements?.Achievements != null && profileWithAchievements.Achievements.Any())
                {
                    _logger.LogInformation("EF Navigation loaded {Count} achievements", profileWithAchievements.Achievements.Count);

                    userAchievements = profileWithAchievements.Achievements.Select(ua => new UserAchievementDTO
                    {
                        Name = ua.Achievement?.Name ?? "Unknown Achievement",
                        Description = ua.Achievement?.Description ?? "",
                        IconUrl = ua.Achievement?.IconUrl ?? "/images/achievements/default.png",
                        EarnedAt = ua.EarnedAt,
                        Progress = ua.Progress,
                        TotalRequired = 100,
                        IsCompleted = ua.Progress >= 100
                    }).ToList();
                }
                else
                {
                    _logger.LogWarning("EF Navigation returned 0 achievements, using manual join...");

                    // Fallback to manual join
                    userAchievements = await _context.UserAchievements
                        .Where(ua => ua.UserId == userId)
                        .Join(
                            _context.Achievements,
                            ua => ua.AchievementId,
                            a => a.AchievementId,
                            (ua, a) => new UserAchievementDTO
                            {
                                Name = a.Name,
                                Description = a.Description,
                                IconUrl = a.IconUrl,
                                EarnedAt = ua.EarnedAt,
                                Progress = ua.Progress,
                                TotalRequired = 100,
                                IsCompleted = ua.Progress >= 100
                            })
                        .ToListAsync();

                    _logger.LogInformation("Manual join loaded {Count} achievements", userAchievements.Count);
                }

                // STEP 3: Auto-check for achievements if user has activity but few achievements
                if (profile.ExperiencePoints > 100 && userAchievements.Count < 3)
                {
                    _logger.LogInformation("User has activity (XP: {XP}) but only {AchievementCount} achievements. Checking for additional achievements...",
                        profile.ExperiencePoints, userAchievements.Count);

                    await CheckAndAwardAchievementsAsync(userId);

                    // Reload achievements after checking
                    var updatedAchievements = await _context.UserAchievements
                        .Where(ua => ua.UserId == userId)
                        .Join(
                            _context.Achievements,
                            ua => ua.AchievementId,
                            a => a.AchievementId,
                            (ua, a) => new UserAchievementDTO
                            {
                                Name = a.Name,
                                Description = a.Description,
                                IconUrl = a.IconUrl,
                                EarnedAt = ua.EarnedAt,
                                Progress = ua.Progress,
                                TotalRequired = 100,
                                IsCompleted = ua.Progress >= 100
                            })
                        .ToListAsync();

                    if (updatedAchievements.Count > userAchievements.Count)
                    {
                        _logger.LogInformation("Auto-awarded {NewAchievements} additional achievements",
                            updatedAchievements.Count - userAchievements.Count);
                        userAchievements = updatedAchievements;
                    }
                }

                // STEP 4: Calculate level progression
                var pointsForNextLevel = CalculatePointsForLevel(profile.Level + 1);
                var pointsForCurrentLevel = CalculatePointsForLevel(profile.Level);
                var levelProgress = pointsForNextLevel > pointsForCurrentLevel
                    ? (decimal)(profile.ExperiencePoints - pointsForCurrentLevel) / (pointsForNextLevel - pointsForCurrentLevel)
                    : 0;

                levelProgress = Math.Max(0, Math.Min(1, levelProgress));

                _logger.LogDebug("Level progress: {LevelProgress:P2} (Current: {CurrentXP}, Next Level: {NextLevelXP})",
                    levelProgress, profile.ExperiencePoints, pointsForNextLevel);

                // STEP 5: Build final DTO
                var profileDto = new GamificationProfileDTO
                {
                    UserId = profile.UserId,
                    ExperiencePoints = profile.ExperiencePoints,
                    Level = profile.Level,
                    CurrentRank = GetRankForLevel(profile.Level),
                    StreakCount = profile.StreakCount,
                    PointsToNextLevel = Math.Max(0, pointsForNextLevel - profile.ExperiencePoints),
                    LevelProgress = levelProgress,
                    Achievements = userAchievements
                };

                _logger.LogInformation("=== FINAL RESULT: User {UserId} has {AchievementsCount} achievements ===",
                    userId, profileDto.Achievements.Count);

                if (profileDto.Achievements.Count > 0)
                {
                    _logger.LogInformation("Achievements summary:");
                    foreach (var achievement in profileDto.Achievements.Take(5))
                    {
                        _logger.LogInformation("  - {Name} (Completed: {IsCompleted}, Progress: {Progress}%)",
                            achievement.Name, achievement.IsCompleted, achievement.Progress);
                    }

                    if (profileDto.Achievements.Count > 5)
                    {
                        _logger.LogInformation("  ... and {Count} more achievements", profileDto.Achievements.Count - 5);
                    }
                }
                else
                {
                    _logger.LogWarning("User {UserId} has NO achievements despite profile existing", userId);
                }

                return profileDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetUserProfileAsync for user {UserId}", userId);

                // Return basic profile but log the error
                return new GamificationProfileDTO
                {
                    UserId = userId,
                    ExperiencePoints = 0,
                    Level = 1,
                    CurrentRank = "Beginner",
                    StreakCount = 0,
                    Achievements = new List<UserAchievementDTO>(),
                    PointsToNextLevel = 100,
                    LevelProgress = 0
                };
            }
        }

        public async Task<List<Achievement>> GetAvailableAchievementsAsync()
        {
            return await _context.Achievements.ToListAsync();
        }

        public async Task<bool> TestAchievementCriteria(int userId, int achievementId)
        {
            var achievement = await _context.Achievements.FindAsync(achievementId);
            if (achievement == null) return false;

            var profile = await _context.GamificationProfiles
                .Include(p => p.Achievements)
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile == null) return false;

            return await CheckAchievementCriteriaAsync(userId, achievement, profile);
        }

        // NEW METHODS FOR XP BREAKDOWN AND ACTIVITY
        public async Task<XPBreakdownDTO> GetXPBreakdownAsync(int userId)
        {
            _logger.LogInformation("Getting XP breakdown for user {UserId}", userId);

            try
            {
                var breakdown = new XPBreakdownDTO();

                // 1. Get XP from completed sessions
                var user = await _context.Users
                    .Include(u => u.Student)
                    .Include(u => u.Tutor)
                    .FirstOrDefaultAsync(u => u.UserId == userId);

                if (user != null)
                {
                    if (user.Role == "Student" && user.Student != null)
                    {
                        var studentSessions = await _context.Bookings
                            .CountAsync(b => b.StudentId == user.Student.StudentId && b.Status == "Completed");
                        breakdown.Sessions = studentSessions * 50; // 50 XP per session
                    }
                    else if (user.Role == "Tutor" && user.Tutor != null)
                    {
                        var tutorSessions = await _context.Bookings
                            .CountAsync(b => b.TutorId == user.Tutor.TutorId && b.Status == "Completed");
                        breakdown.Sessions = tutorSessions * 50; // 50 XP per session
                    }
                }

                // 2. Get XP from achievements
                var achievementXP = await _context.UserAchievements
                    .Where(ua => ua.UserId == userId && ua.Progress >= 100)
                    .Join(_context.Achievements,
                          ua => ua.AchievementId,
                          a => a.AchievementId,
                          (ua, a) => a.PointsReward)
                    .SumAsync();

                breakdown.Achievements = achievementXP;

                // 3. Estimate daily login XP (this is approximate)
                var profile = await _context.GamificationProfiles
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (profile != null)
                {
                    // Estimate based on streak and level - adjust this logic as needed
                    breakdown.DailyLogin = profile.StreakCount * 10; // 10 XP per login day in streak
                }

                // 4. Get XP from bonuses (reviews, etc.)
                if (user?.Tutor != null)
                {
                    var bonusReviews = await _context.Reviews
                        .CountAsync(r => r.TutorId == user.Tutor.TutorId && r.Rating == 5);
                    breakdown.Bonuses = bonusReviews * 25; // 25 XP per 5-star review
                }

                // 5. Calculate "Other" XP (difference between total and calculated)
                var totalCalculated = breakdown.Sessions + breakdown.Achievements +
                                    breakdown.DailyLogin + breakdown.Bonuses;
                breakdown.Other = Math.Max(0, (profile?.ExperiencePoints ?? 0) - totalCalculated);

                // 6. Set total
                breakdown.Total = profile?.ExperiencePoints ?? 0;

                _logger.LogInformation("XP Breakdown for user {UserId}: Sessions={Sessions}, Achievements={Achievements}, DailyLogin={DailyLogin}, Bonuses={Bonuses}, Other={Other}, Total={Total}",
                    userId, breakdown.Sessions, breakdown.Achievements, breakdown.DailyLogin,
                    breakdown.Bonuses, breakdown.Other, breakdown.Total);

                return breakdown;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting XP breakdown for user {UserId}", userId);
                return new XPBreakdownDTO();
            }
        }

        public async Task<List<XPActivityDTO>> GetRecentXPActivityAsync(int userId)
        {
            _logger.LogInformation("Getting recent XP activity for user {UserId}", userId);

            try
            {
                var activities = new List<XPActivityDTO>();
                var now = DateTime.UtcNow;

                // 1. Get recent completed sessions (last 30 days)
                var user = await _context.Users
                    .Include(u => u.Student)
                    .Include(u => u.Tutor)
                    .FirstOrDefaultAsync(u => u.UserId == userId);

                if (user != null)
                {
                    IQueryable<Booking> sessionsQuery = user.Role == "Student" && user.Student != null
                        ? _context.Bookings.Where(b => b.StudentId == user.Student.StudentId)
                        : user.Role == "Tutor" && user.Tutor != null
                            ? _context.Bookings.Where(b => b.TutorId == user.Tutor.TutorId)
                            : _context.Bookings.Where(b => false);

                    var recentSessions = await sessionsQuery
                        .Where(b => b.Status == "Completed" && b.EndTime >= now.AddDays(-30))
                        .Include(b => b.Module)
                        .OrderByDescending(b => b.EndTime)
                        .Take(10)
                        .ToListAsync();

                    foreach (var session in recentSessions)
                    {
                        activities.Add(new XPActivityDTO
                        {
                            Type = "Session",
                            Description = $"Completed {session.Module?.Name ?? "session"} with " +
                                         (user.Role == "Student" ? "tutor" : "student"),
                            Points = 50,
                            Timestamp = session.EndTime == default
    ? session.StartTime
    : session.EndTime,

                            Icon = "fa-calendar-check",
                            Color = "bg-success"
                        });
                    }
                }

                // 2. Get recent achievements (last 30 days)
                var recentAchievements = await _context.UserAchievements
                    .Where(ua => ua.UserId == userId && ua.Progress >= 100 && ua.EarnedAt >= now.AddDays(-30))
                    .Include(ua => ua.Achievement)
                    .OrderByDescending(ua => ua.EarnedAt)
                    .Take(10)
                    .ToListAsync();

                foreach (var userAchievement in recentAchievements)
                {
                    activities.Add(new XPActivityDTO
                    {
                        Type = "Achievement",
                        Description = $"Unlocked '{userAchievement.Achievement?.Name}'",
                        Points = userAchievement.Achievement?.PointsReward ?? 0,
                        Timestamp = userAchievement.EarnedAt,
                        Icon = "fa-trophy",
                        Color = "bg-warning"
                    });
                }

                // 3. Add daily login activities (simulated - you might want to track these separately)
                var profile = await _context.GamificationProfiles
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (profile != null && profile.StreakCount > 0)
                {
                    // Add recent login activities (last 7 days)
                    for (int i = 0; i < Math.Min(7, profile.StreakCount); i++)
                    {
                        activities.Add(new XPActivityDTO
                        {
                            Type = "DailyLogin",
                            Description = "Daily login bonus",
                            Points = 10,
                            Timestamp = now.AddDays(-i),
                            Icon = "fa-sign-in-alt",
                            Color = "bg-info"
                        });
                    }
                }

                // 4. Add bonus activities (reviews, etc.)
                if (user?.Tutor != null)
                {
                    var recentReviews = await _context.Reviews
                        .Where(r => r.TutorId == user.Tutor.TutorId && r.Rating == 5 && r.CreatedDate >= now.AddDays(-30))
                        .OrderByDescending(r => r.CreatedDate)
                        .Take(5)
                        .ToListAsync();

                    foreach (var review in recentReviews)
                    {
                        activities.Add(new XPActivityDTO
                        {
                            Type = "Bonus",
                            Description = "Received 5-star review",
                            Points = 25,
                            Timestamp = review.CreatedDate,
                            Icon = "fa-star",
                            Color = "bg-primary"
                        });
                    }
                }

                // Sort all activities by timestamp and return top 10 most recent
                return activities
                    .OrderByDescending(a => a.Timestamp)
                    .Take(10)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent XP activity for user {UserId}", userId);
                return new List<XPActivityDTO>();
            }
        }

        // Helper methods
        private int CalculateLevel(int experiencePoints)
        {
            return (int)Math.Floor(Math.Sqrt(experiencePoints / 100.0)) + 1;
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

        private async Task<bool> CheckAchievementCriteriaAsync(int userId, Achievement achievement, GamificationProfile profile)
        {
            // Your existing implementation
            try
            {
                if (string.IsNullOrEmpty(achievement.Criteria))
                {
                    _logger.LogWarning("Achievement {AchievementId} has no criteria", achievement.AchievementId);
                    return false;
                }

                var criteria = System.Text.Json.JsonSerializer.Deserialize<AchievementRequirement>(
                    achievement.Criteria,
                    new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
                    });

                if (criteria == null)
                {
                    _logger.LogWarning("Failed to deserialize criteria for achievement {AchievementId}", achievement.AchievementId);
                    return false;
                }

                return achievement.Type switch
                {
                    "Attendance" => await CheckAttendanceAchievement(userId, achievement, profile, criteria),
                    "Progress" => await CheckProgressAchievement(userId, achievement, profile, criteria),
                    "Mastery" => await CheckMasteryAchievement(userId, achievement, profile, criteria),
                    "Social" => await CheckSocialAchievement(userId, achievement, profile, criteria),
                    _ => false
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking achievement criteria for user {UserId}, achievement {AchievementId}",
                    userId, achievement.AchievementId);
                return false;
            }
        }

        private async Task<bool> CheckAttendanceAchievement(int userId, Achievement achievement, GamificationProfile profile, AchievementRequirement criteria)
        {
            try
            {
                _logger.LogDebug("Checking attendance achievement for user {UserId}, criteria: {CriteriaType}", userId, criteria.CriteriaType);

                switch (criteria.CriteriaType)
                {
                    case "account_created":
                        return profile.ExperiencePoints > 0;

                    case "email_verified":
                        return profile.ExperiencePoints >= 150;

                    case "session_count":
                        // FIXED: Get the actual StudentId or TutorId for this user
                        var user = await _context.Users
                            .Include(u => u.Student)
                            .Include(u => u.Tutor)
                            .FirstOrDefaultAsync(u => u.UserId == userId);

                        if (user == null) return false;

                        int sessionCount = 0;

                        if (user.Role == "Student" && user.Student != null)
                        {
                            sessionCount = await _context.Bookings
                                .CountAsync(b => b.StudentId == user.Student.StudentId && b.Status == "Completed");
                        }
                        else if (user.Role == "Tutor" && user.Tutor != null)
                        {
                            sessionCount = await _context.Bookings
                                .CountAsync(b => b.TutorId == user.Tutor.TutorId && b.Status == "Completed");
                        }

                        _logger.LogDebug("User {UserId} (Role: {Role}) has {SessionCount} completed sessions (needs {Required})",
                            userId, user.Role, sessionCount, criteria.RequiredCount);
                        return sessionCount >= criteria.RequiredCount;

                    case "login_streak":
                        _logger.LogDebug("User {UserId} has streak {Streak} (needs {Required})",
                            userId, profile.StreakCount, criteria.RequiredCount);
                        return profile.StreakCount >= criteria.RequiredCount;

                    default:
                        _logger.LogWarning("Unknown attendance criteria type: {CriteriaType}", criteria.CriteriaType);
                        return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking attendance achievement for user {UserId}", userId);
                return false;
            }
        }


        private async Task<bool> CheckProgressAchievement(int userId, Achievement achievement, GamificationProfile profile, AchievementRequirement criteria)
        {
            try
            {
                _logger.LogDebug("Checking progress achievement for user {UserId}, criteria: {CriteriaType}", userId, criteria.CriteriaType);

                // FIXED: Get user with their role-specific ID first
                var user = await _context.Users
                    .Include(u => u.Student)
                    .Include(u => u.Tutor)
                    .FirstOrDefaultAsync(u => u.UserId == userId);

                if (user == null) return false;

                switch (criteria.CriteriaType)
                {
                    case "reach_level":
                        _logger.LogDebug("User {UserId} is level {CurrentLevel} (needs {Required})",
                            userId, profile.Level, criteria.RequiredCount);
                        return profile.Level >= criteria.RequiredCount;

                    case "module_sessions":
                        // FIXED: Use the correct ID based on user role
                        IQueryable<Booking> bookingsQuery = user.Role == "Student" && user.Student != null
                            ? _context.Bookings.Where(b => b.StudentId == user.Student.StudentId)
                            : user.Role == "Tutor" && user.Tutor != null
                                ? _context.Bookings.Where(b => b.TutorId == user.Tutor.TutorId)
                                : _context.Bookings.Where(b => false); // No valid role

                        var moduleSessionCounts = await bookingsQuery
                            .Where(b => b.Status == "Completed" && b.ModuleId != null)
                            .GroupBy(b => b.ModuleId)
                            .Select(g => new { ModuleId = g.Key, Count = g.Count() })
                            .ToListAsync();

                        var hasModuleWithEnoughSessions = moduleSessionCounts.Any(g => g.Count >= criteria.RequiredCount);
                        _logger.LogDebug("User {UserId} has module with {Required} sessions: {Result}",
                            userId, criteria.RequiredCount, hasModuleWithEnoughSessions);
                        return hasModuleWithEnoughSessions;

                    case "unique_modules":
                        // FIXED: Use the correct ID based on user role
                        IQueryable<Booking> uniqueModulesQuery = user.Role == "Student" && user.Student != null
                            ? _context.Bookings.Where(b => b.StudentId == user.Student.StudentId)
                            : user.Role == "Tutor" && user.Tutor != null
                                ? _context.Bookings.Where(b => b.TutorId == user.Tutor.TutorId)
                                : _context.Bookings.Where(b => false);

                        var uniqueModulesCount = await uniqueModulesQuery
                            .Where(b => b.Status == "Completed" && b.ModuleId != null)
                            .Select(b => b.ModuleId)
                            .Distinct()
                            .CountAsync();

                        _logger.LogDebug("User {UserId} has {UniqueModules} unique modules (needs {Required})",
                            userId, uniqueModulesCount, criteria.RequiredCount);
                        return uniqueModulesCount >= criteria.RequiredCount;

                    default:
                        _logger.LogWarning("Unknown progress criteria type: {CriteriaType}", criteria.CriteriaType);
                        return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking progress achievement for user {UserId}", userId);
                return false;
            }
        }



        private async Task<bool> CheckMasteryAchievement(int userId, Achievement achievement, GamificationProfile profile, AchievementRequirement criteria)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Tutor)
                    .FirstOrDefaultAsync(u => u.UserId == userId);

                if (user?.Tutor == null) return false; // Only tutors can earn mastery achievements

                // FIXED: Use TutorId instead of UserId
                var tutorId = user.Tutor.TutorId;

                switch (criteria.CriteriaType)
                {
                    case "five_star_rating":
                        var hasFiveStarRating = await _context.Reviews
                            .AnyAsync(r => r.TutorId == tutorId && r.Rating == 5);
                        return hasFiveStarRating;

                    case "five_star_ratings":
                        var fiveStarCount = await _context.Reviews
                            .CountAsync(r => r.TutorId == tutorId && r.Rating == 5);
                        return fiveStarCount >= criteria.RequiredCount;

                    case "high_rating_average":
                        var ratingAverage = await _context.Reviews
                            .Where(r => r.TutorId == tutorId)
                            .AverageAsync(r => (double?)r.Rating) ?? 0;
                        return ratingAverage >= criteria.RequiredCount;

                    default:
                        _logger.LogWarning("Unknown mastery criteria type: {CriteriaType}", criteria.CriteriaType);
                        return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking mastery achievement for user {UserId}", userId);
                return false;
            }
        }



        private async Task<bool> CheckSocialAchievement(int userId, Achievement achievement, GamificationProfile profile, AchievementRequirement criteria)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Student)
                    .Include(u => u.Tutor)
                    .FirstOrDefaultAsync(u => u.UserId == userId);

                if (user == null) return false;

                switch (criteria.CriteriaType)
                {
                    case "unique_students":
                        if (user.Tutor == null) return false;
                        var uniqueStudentsCount = await _context.Bookings
                            .Where(b => b.TutorId == user.Tutor.TutorId && b.Status == "Completed")
                            .Select(b => b.StudentId)
                            .Distinct()
                            .CountAsync();
                        return uniqueStudentsCount >= criteria.RequiredCount;

                    case "questions_asked":
                        if (user.Student == null) return false;
                        var sessionsWithQuestions = await _context.Bookings
                            .CountAsync(b => b.StudentId == user.Student.StudentId &&
                                           b.Status == "Completed" &&
                                           !string.IsNullOrEmpty(b.Notes));
                        return sessionsWithQuestions >= criteria.RequiredCount;

                    case "join_study_group":
                    case "board_posts":
                        // Not implemented yet
                        return false;

                    default:
                        _logger.LogWarning("Unknown social criteria type: {CriteriaType}", criteria.CriteriaType);
                        return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking social achievement for user {UserId}", userId);
                return false;
            }
        }
    }
}