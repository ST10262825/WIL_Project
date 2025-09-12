namespace TutorConnect.WebApp.Models
{
    public class CreateBookingDTO
    {
        public int TutorId { get; set; }
        public int StudentId { get; set; }
        public int ModuleId { get; set; }
        public DateTime SessionDate { get; set; }
        public string? Notes { get; set; }
    }

}
