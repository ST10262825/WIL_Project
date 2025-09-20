namespace TutorConnectAPI.DTOs
{
    public class StudentDashboardSummaryDTO
    {
        public int StudentId { get; set; }
        public int UpcomingSessionsCount { get; set; }
        public int TotalLearningHours { get; set; }
        public int CompletedSessionsCount { get; set; }
        public int ActiveTutorsCount { get; set; }
        public int PendingBookingsCount { get; set; }
    }
}
