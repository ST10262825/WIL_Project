// Models/GamificationProfileViewModel.cs
namespace TutorConnect.WebApp.Models
{
    public class GamificationProfileViewModel
    {
        public GamificationProfileDTO Profile { get; set; }
        public List<Achievement> AllAchievements { get; set; }
        public List<ActivityItem> RecentActivity { get; set; }


        public XPBreakdownDTO XPBreakdown { get; set; }
        public List<XPActivityDTO> RecentXPActivity { get; set; }

    }

    public class AchievementsViewModel
    {
        public GamificationProfileDTO Profile { get; set; }
        public List<Achievement> Achievements { get; set; }
        public int EarnedCount { get; set; }
        public int TotalCount { get; set; }
    }

    public class ActivityItem
    {
        public string Type { get; set; }
        public string Description { get; set; }
        public int Points { get; set; }
        public DateTime Date { get; set; }
        public string Icon { get; set; }
    }


}