namespace TutorConnectAPI.DTOs
{
    public class SessionBookingDTO
    {
        public int StudentId { get; set; }
        public int TutorId { get; set; }
        public int ModuleId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }

}
