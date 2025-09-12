namespace TutorConnectAPI.DTOs
{
    public class UpdateSessionStatusDTO
    {
        public string Status { get; set; } = "";
        public string? RejectionReason { get; set; }
    }

}
