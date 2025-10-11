using TutorConnectAPI.Models;

namespace TutorConnectAPI.Data
{
    // Data/Seeders/AchievementSeeder.cs - UPDATED
    public static class AchievementSeeder
    {
        public static void SeedAchievements(ApplicationDbContext context)
        {
            if (context.Achievements.Any())
            {
                return; // Database has been seeded
            }

            var achievements = new List<Achievement>
        {
            // ===== REGISTRATION & ONBOARDING ACHIEVEMENTS =====
            new Achievement
            {
                Name = "Getting Started",
                Description = "Create your TutorConnect account",
                IconUrl = "/images/achievements/getting-started.png",
                Type = "Attendance",
                PointsReward = 50,
                Criteria = "{\"CriteriaType\":\"account_created\",\"RequiredCount\":1}"
            },
            new Achievement
            {
                Name = "Verified Scholar",
                Description = "Verify your email address",
                IconUrl = "/images/achievements/verified-scholar.png",
                Type = "Attendance",
                PointsReward = 50,
                Criteria = "{\"CriteriaType\":\"email_verified\",\"RequiredCount\":1}"
            },

            // ===== SESSION-BASED ACHIEVEMENTS =====
            new Achievement
            {
                Name = "First Session",
                Description = "Complete your first tutoring session",
                IconUrl = "/images/achievements/first-session.png",
                Type = "Attendance",
                PointsReward = 100,
                Criteria = "{\"CriteriaType\":\"session_count\",\"RequiredCount\":1}"
            },
            new Achievement
            {
                Name = "Dedicated Learner",
                Description = "Complete 5 tutoring sessions",
                IconUrl = "/images/achievements/dedicated-learner.png",
                Type = "Attendance",
                PointsReward = 250,
                Criteria = "{\"CriteriaType\":\"session_count\",\"RequiredCount\":5}"
            },
            new Achievement
            {
                Name = "Learning Marathon",
                Description = "Complete 10 tutoring sessions",
                IconUrl = "/images/achievements/learning-marathon.png",
                Type = "Attendance",
                PointsReward = 500,
                Criteria = "{\"CriteriaType\":\"session_count\",\"RequiredCount\":10}"
            },

            // ===== DAILY ACTIVITY ACHIEVEMENTS =====
            new Achievement
            {
                Name = "Daily Visitor",
                Description = "Log in for 3 consecutive days",
                IconUrl = "/images/achievements/daily-visitor.png",
                Type = "Attendance",
                PointsReward = 100,
                Criteria = "{\"CriteriaType\":\"login_streak\",\"RequiredCount\":3}"
            },
            new Achievement
            {
                Name = "Week Warrior",
                Description = "Log in for 7 consecutive days",
                IconUrl = "/images/achievements/week-warrior.png",
                Type = "Attendance",
                PointsReward = 250,
                Criteria = "{\"CriteriaType\":\"login_streak\",\"RequiredCount\":7}"
            },

            // ===== PROGRESS ACHIEVEMENTS =====
            new Achievement
            {
                Name = "Quick Learner",
                Description = "Complete 3 sessions in the same subject",
                IconUrl = "/images/achievements/quick-learner.png",
                Type = "Progress",
                PointsReward = 200,
                Criteria = "{\"CriteriaType\":\"module_sessions\",\"RequiredCount\":3}"
            },
            new Achievement
            {
                Name = "Subject Explorer",
                Description = "Study 3 different subjects",
                IconUrl = "/images/achievements/subject-explorer.png",
                Type = "Progress",
                PointsReward = 300,
                Criteria = "{\"CriteriaType\":\"unique_modules\",\"RequiredCount\":3}"
            },

            // ===== LEVEL-BASED ACHIEVEMENTS =====
            new Achievement
            {
                Name = "Level 5 Achiever",
                Description = "Reach level 5 in your learning journey",
                IconUrl = "/images/achievements/level-5-achiever.png",
                Type = "Progress",
                PointsReward = 200,
                Criteria = "{\"CriteriaType\":\"reach_level\",\"RequiredCount\":5}"
            },
            new Achievement
            {
                Name = "Level 10 Expert",
                Description = "Reach level 10 in your learning journey",
                IconUrl = "/images/achievements/level-10-expert.png",
                Type = "Progress",
                PointsReward = 500,
                Criteria = "{\"CriteriaType\":\"reach_level\",\"RequiredCount\":10}"
            },

            // ===== TUTOR-SPECIFIC ACHIEVEMENTS =====
            new Achievement
            {
                Name = "First Five Stars",
                Description = "Receive your first 5-star rating as a tutor",
                IconUrl = "/images/achievements/first-five-stars.png",
                Type = "Mastery",
                PointsReward = 200,
                Criteria = "{\"CriteriaType\":\"five_star_rating\",\"RequiredCount\":1}"
            },
            new Achievement
            {
                Name = "Top Rated Tutor",
                Description = "Receive 5 five-star ratings",
                IconUrl = "/images/achievements/top-rated-tutor.png",
                Type = "Mastery",
                PointsReward = 500,
                Criteria = "{\"CriteriaType\":\"five_star_ratings\",\"RequiredCount\":5}"
            }
        };

            context.Achievements.AddRange(achievements);
            context.SaveChanges();
        }
    }
}
