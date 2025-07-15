namespace TutorConnectAPI.Models
{
    public class TutorModule
    {
        public int TutorId { get; set; }
        public Tutor Tutor { get; set; }
        public int ModuleId { get; set; }
        public Module Module { get; set; }
    }
}