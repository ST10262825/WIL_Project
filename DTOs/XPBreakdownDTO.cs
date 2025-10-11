namespace TutorConnectAPI.DTOs
{
    public class XPBreakdownDTO
    {
        public int Sessions { get; set; }      // XP from completed sessions
        public int Achievements { get; set; }  // XP from achievements
        public int DailyLogin { get; set; }    // XP from daily logins
        public int Bonuses { get; set; }       // XP from bonuses/reviews
        public int Other { get; set; }         // XP from other activities
        public int Total { get; set; }         // Total XP (calculated)
    }

    public class XPActivityDTO
    {
        public string Type { get; set; }       // "Session", "Achievement", "DailyLogin", "Bonus"
        public string Description { get; set; }
        public int Points { get; set; }
        public DateTime Timestamp { get; set; }
        public string Icon { get; set; }       // FontAwesome icon class
        public string Color { get; set; }      // Bootstrap color class
    }
}