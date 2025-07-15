namespace TutorConnect.WebApp.Models
{
    public class SessionSummaryDTO
    {
        public int Total { get; set; }
        public int Pending { get; set; }
        public int Approved { get; set; }
        public int Rejected { get; set; }
    }
}
