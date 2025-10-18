namespace TutorConnectAPI.Models
{
    public class Course
    {
        public int CourseId { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public ICollection<Enrollment> Enrollments { get; set; }

        // Navigation properties
        public ICollection<Module> Modules { get; set; } = new List<Module>();
        public ICollection<Tutor> Tutors { get; set; } = new List<Tutor>();
        public ICollection<Student> Students { get; set; } = new List<Student>();
    }
}
