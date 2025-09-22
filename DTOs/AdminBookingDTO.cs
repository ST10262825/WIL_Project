namespace TutorConnectAPI.DTOs
{
    public class AdminBookingDTO
    {
        public int BookingId { get; set; }
        public string TutorName { get; set; }
        public string StudentName { get; set; }
        public string ModuleName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Status { get; set; }
        public string Notes { get; set; }
        public double DurationHours { get; set; }
    }
}
