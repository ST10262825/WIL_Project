namespace TutorConnect.WebApp.Models
{
    public class CreateBookingDTO
    {
        public int TutorId { get; set; }
        public int StudentId { get; set; }
        public int ModuleId { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public string? Notes { get; set; }
    }

}
