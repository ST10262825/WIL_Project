namespace TutorConnect.WebApp.Models
{
    public class ReviewDTO
    {
        public int ReviewId { get; set; }
        public int BookingId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsVerified { get; set; }
    }
}