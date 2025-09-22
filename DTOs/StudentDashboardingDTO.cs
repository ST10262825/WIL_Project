namespace TutorConnectAPI.DTOs
{
    public class StudentDashboardDTO
    {
        public int StudentId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public bool IsBlocked { get; set; }
        public int TotalBookings { get; set; }
        public int CompletedBookings { get; set; }
        public DateTime JoinDate { get; set; }
    }
}
