namespace TutorConnect.WebApp.Models
{
    public class DashboardStatsDTO
    {
        public int TotalTutors { get; set; }
        public int TotalStudents { get; set; }
        public int TotalBookings { get; set; }
        public int PendingBookings { get; set; }
        public int ActiveModules { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}
