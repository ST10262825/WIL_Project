namespace TutorConnectAPI.DTOs
{
    public class GamificationProfileDTO
    {
        public int UserId { get; set; }
        public int ExperiencePoints { get; set; }
        public int Level { get; set; }
        public string CurrentRank { get; set; }
        public int StreakCount { get; set; }
        public List<UserAchievementDTO> Achievements { get; set; } = new();
        public int PointsToNextLevel { get; set; }
        public decimal LevelProgress { get; set; }
    }

    public class UserAchievementDTO
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string IconUrl { get; set; }
        public DateTime EarnedAt { get; set; }
        public int Progress { get; set; }
        public int TotalRequired { get; set; }
        public bool IsCompleted { get; set; }
    }



    public class AwardPointsRequest
    {
        public int UserId { get; set; }
        public string ActivityType { get; set; } // "SessionCompleted", "AchievementEarned", "DailyLogin"
        public int Points { get; set; }
        public string Description { get; set; }
    }

    public class ActivityResponse
    {
        public bool Success { get; set; }
        public int PointsAwarded { get; set; }
        public int NewLevel { get; set; }
        public List<string> AchievementsUnlocked { get; set; } = new();
        public string Message { get; set; }
    }
}
