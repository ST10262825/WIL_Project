using Microsoft.EntityFrameworkCore.Migrations;
using System.ComponentModel.DataAnnotations;

namespace TutorConnectAPI.Models
{
    public class Achievement
    {
        [Key]
        public int AchievementId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string IconUrl { get; set; }
        public string Type { get; set; } // "Attendance", "Progress", "Mastery", "Social"
        public int PointsReward { get; set; }
        public string Criteria { get; set; } // JSON for achievement requirements
    }

    public class UserAchievement
    {
        [Key]
        public int UserAchievementId { get; set; }
        public int UserId { get; set; }
        public int AchievementId { get; set; }
        public Achievement Achievement { get; set; }
        public DateTime EarnedAt { get; set; }
        public int Progress { get; set; } = 0; // For progressive achievements

        public int GamificationProfileId { get; set; } // Make it nullable since existing data has NULL
        public GamificationProfile GamificationProfile { get; set; }
    }

    public class VirtualLearningSpace
    {
        [Key]
        public int SpaceId { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
        public string Theme { get; set; } = "default";
        public string? Background { get; set; }
        public List<SpaceItem> Items { get; set; } = new();
        public bool IsPublic { get; set; } = false;
    }

    public class SpaceItem
    {
        [Key]
        public int ItemId { get; set; }
        public int SpaceId { get; set; }
        public string Type { get; set; } // "Trophy", "Plant", "Book", "Poster"
        public string Position { get; set; } // JSON coordinates
        public DateTime UnlockedAt { get; set; }
    }
}
