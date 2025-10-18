namespace TutorConnectAPI.Models
{
    public class Module
    {
        public int ModuleId { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        // Add Course relationship
        public int CourseId { get; set; }
        public Course Course { get; set; }

        public ICollection<Session> Sessions { get; set; }

        public ICollection<Booking> Bookings { get; set; }
        public ICollection<TutorModule> TutorModules { get; set; }

    }
}