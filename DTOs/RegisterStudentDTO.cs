namespace TutorConnectAPI.DTOs
{
    public class RegisterStudentDTO
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string Name { get; set; }
     
        public int CourseId { get; set; }
    }
}