namespace TutorConnectAPI.DTOs
{
    public class StudentSessionDTO
    {
        public int Id { get; set; }
        public string TutorName { get; set; }
        public string ModuleName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Status { get; set; }
    }
}
