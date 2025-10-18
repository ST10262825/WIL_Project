namespace TutorConnect.WebApp.Models
{
    public class CourseDTO
    {
        public int CourseId { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public int ModuleCount { get; set; }
        public int TutorCount { get; set; }
        public int StudentCount { get; set; }
    }

    public class CreateCourseDTO
    {
        public string Title { get; set; }
        public string? Description { get; set; }
    }

    public class UpdateCourseDTO
    {
        public int CourseId { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
    }
}
