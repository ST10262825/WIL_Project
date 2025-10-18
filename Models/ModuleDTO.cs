namespace TutorConnect.WebApp.Models
{
    public class ModuleDTO
    {
        public int ModuleId { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public int CourseId { get; set; } 
        public string CourseName { get; set; }
        public int TutorCount { get; set; }
        public int BookingCount { get; set; }
    }
}

