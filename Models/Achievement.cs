namespace TutorConnect.WebApp.Models
{
    
        public class Achievement
        {
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
            public int UserAchievementId { get; set; }
            public int UserId { get; set; }
            public int AchievementId { get; set; }
            public Achievement Achievement { get; set; }
            public DateTime EarnedAt { get; set; }
            public int Progress { get; set; } = 0; // For progressive achievements
        }

       

        public class SpaceItem
        {
            public int ItemId { get; set; }
            public int SpaceId { get; set; }
            public string Type { get; set; } // "Trophy", "Plant", "Book", "Poster"
            public string Position { get; set; } // JSON coordinates
            public DateTime UnlockedAt { get; set; }
        }
    }


