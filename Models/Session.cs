namespace TutorConnectAPI.Models
{
    public class Session
    {
        public int SessionId { get; set; }
        public string Title { get; set; }
        public DateTime DateTime { get; set; }
        public int StudentId { get; set; }
        public Student Student { get; set; }
        public int ModuleId { get; set; }
        public Module Module { get; set; }
        public int TutorId { get; set; }
        public Tutor Tutor { get; set; }
        public bool IsCompleted { get; set; }
    }
}
