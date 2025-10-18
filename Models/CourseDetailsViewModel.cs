namespace TutorConnect.WebApp.Models
{
    public class CourseDetailsViewModel
    {
        public CourseDTO Course { get; set; }
        public List<ModuleDTO> Modules { get; set; } = new List<ModuleDTO>();
        public List<TutorDTO> Tutors { get; set; } = new List<TutorDTO>();
        public List<StudentDTO> Students { get; set; } = new List<StudentDTO>();
    }
}
