// Add to your WebApp Models (create new file or add to existing)
namespace TutorConnect.WebApp.Models
{
    public class XPBreakdownDTO
    {
        public int Sessions { get; set; }
        public int Achievements { get; set; }
        public int DailyLogin { get; set; }
        public int Bonuses { get; set; }
        public int Other { get; set; }
        public int Total { get; set; }
    }

    public class XPActivityDTO
    {
        public string Type { get; set; }
        public string Description { get; set; }
        public int Points { get; set; }
        public DateTime Timestamp { get; set; }
        public string Icon { get; set; }
        public string Color { get; set; }
    }
}