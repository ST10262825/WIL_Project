namespace TutorConnectAPI.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string Role { get; set; }
        public bool IsActive { get; set; } = true;

        public bool IsEmailVerified { get; set; } = false;
        public string? VerificationToken { get; set; }
    }
}