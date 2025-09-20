namespace TutorConnectAPI.DTOs
{
    public class StudentProgressDTO
    {
        public string ModuleName { get; set; }
        public int TotalSessions { get; set; }
        public int CompletedSessions { get; set; }
        public int PercentageComplete { get; set; }
    }
}
