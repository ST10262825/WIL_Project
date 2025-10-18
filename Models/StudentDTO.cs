namespace TutorConnect.WebApp.Models
{
    public class StudentDTO
    {
        public int StudentId { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; }
        public int CourseId { get; set; }
        public string CourseName { get; set; }
        public string? Bio { get; set; }
        public bool IsBlocked { get; set; }
        public string? ProfileImage { get; set; }
 
       
    }
}
