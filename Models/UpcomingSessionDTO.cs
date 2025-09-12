namespace TutorConnect.WebApp.Models
{
    public class UpcomingSessionDTO
    {
        public int BookingId { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; }
        public string ModuleName { get; set; }
        public DateTime SessionDate { get; set; }
        public string Status { get; set; }
    }
}
