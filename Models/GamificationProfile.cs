namespace TutorConnectAPI.Models
{
    public class GamificationProfile
    {
        public int GamificationProfileId { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }

        public int ExperiencePoints { get; set; }
        public int Level { get; set; } = 1;
        public string CurrentRank { get; set; } = "Beginner";
        public int StreakCount { get; set; }
        public DateTime LastActivityDate { get; set; } = DateTime.UtcNow;
        public List<UserAchievement> Achievements { get; set; } = new();
    }
}
