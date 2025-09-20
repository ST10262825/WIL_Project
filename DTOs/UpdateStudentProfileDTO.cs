namespace TutorConnectAPI.DTOs
{
    public class UpdateStudentProfileDTO
    {
        public string? Bio { get; set; }
        public IFormFile? ProfileImage { get; set; }
    }
}
