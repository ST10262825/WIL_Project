namespace TutorConnect.WebApp.Models
{
    public class BookingStatusUpdateModel
    {
        public int BookingId { get; set; }
        public string Status { get; set; }
        public string? Reason { get; set; }
    }
}
