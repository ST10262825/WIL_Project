namespace TutorConnectAPI.DTOs
{

    public class TutorProfileUpdateDto
    {
        public string Bio { get; set; } = string.Empty;
        public IFormFile? ProfileImage { get; set; }

        public string? AboutMe { get; set; }
        public string? Expertise { get; set; }
        public string? Education { get; set; }
    }
}


