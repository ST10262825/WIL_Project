namespace TutorConnect.WebApp.Models
{
    public class BookingDTO
    {
        public int BookingId { get; set; }
        public int TutorId { get; set; }
        public string TutorName { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; }
        public string ModuleName { get; set; }
        public DateTime SessionDate { get; set; }
        public string Status { get; set; }
        public string? Notes { get; set; }
    }


}
