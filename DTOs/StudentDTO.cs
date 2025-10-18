namespace TutorConnectAPI.DTOs
{
    public class StudentDTO
    {
        public int StudentId { get; set; }    // Primary key of the Student
        public int UserId { get; set; }       // FK to User table
        public string Name { get; set; }      // Student's full name
        public string Course { get; set; }    // Student's course/program
        public string? Bio { get; set; }
        public string? ProfileImage { get; set; }
        public int CourseId { get; set; } 
        public string CourseName { get; set; }
    }
}

