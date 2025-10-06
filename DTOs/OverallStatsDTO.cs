namespace TutorConnectAPI.DTOs
{
    public class OverallStatsDTO
    {
        public int TotalStudents { get; set; }
        public int TotalTutors { get; set; }
        public int TotalBookings { get; set; }
        public int TotalModules { get; set; }
        public int ActiveUsers { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageRating { get; set; }
    }
}
