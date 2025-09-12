namespace TutorConnect.WebApp.Models
{
    public class BrowseTutorDTO
    {
        public int TutorId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string ProfileImageUrl { get; set; } = "/images/default-profile.png";
        public string AboutMe { get; set; } = string.Empty;
        public string Expertise { get; set; } = string.Empty;
        public string Education { get; set; } = string.Empty;

        public List<string> Subjects { get; set; } = new();
        public bool IsVerified { get; set; } = true; // optional, for badge
    }
}
