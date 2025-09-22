namespace TutorConnect.WebApp.Models
{
    public class PendingReviewDTO
    {
        public int BookingId { get; set; }
        public int TutorId { get; set; }
        public string TutorName { get; set; } = string.Empty;
        public string ModuleName { get; set; } = string.Empty;
        public DateTime SessionDate { get; set; }
        public string? TutorProfileImageUrl { get; set; }
    }
}