namespace TutorConnectAPI.Models
{
    public enum SessionStatus
    {
        Pending,
        Approved,
        Rejected,
        Completed
    }

    public class Session
    {
        public int Id { get; set; }

        public int StudentId { get; set; }
        public Student Student { get; set; }

        public int TutorId { get; set; }
        public Tutor Tutor { get; set; }

        public int ModuleId { get; set; }
        public Module Module { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        // Replaces IsConfirmed
        public SessionStatus Status { get; set; } = SessionStatus.Pending;

        // Reason for rejection (optional)
        public string? RejectionReason { get; set; }

        // Feedback after session is completed (optional)
        public string? TutorFeedback { get; set; }
    }
}
