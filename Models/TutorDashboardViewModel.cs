namespace TutorConnect.WebApp.Models
{
    public class TutorDashboardViewModel
    {
        public int ActiveSessionsCount { get; set; }
        public int TotalStudentsCount { get; set; }
        public int PendingBookingsCount { get; set; }
        public int CompletedSessionsCount { get; set; }
        public int ProfileCompletion { get; set; }
        public List<UpcomingSessionDTO> UpcomingSessions { get; set; }
        public int UnreadMessagesCount { get; set; }
    }
}
