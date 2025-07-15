namespace TutorConnect.WebApp.Models
{
    public class SessionDTO
    {
        public int Id { get; set; }
        public string StudentName { get; set; }
        public string ModuleName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Status { get; set; }
    }
}
