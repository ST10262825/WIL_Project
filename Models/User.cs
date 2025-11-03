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

        public string? PasswordResetToken { get; set; }
        public DateTime? ResetTokenExpires { get; set; }

        // ✅ ADD THEME PREFERENCE
        public string ThemePreference { get; set; } = "light"; // Default to light theme


        // Navigation properties
        public Student? Student { get; set; }
        public Tutor? Tutor { get; set; }

        // Helper property to check if user is blocked
        public bool IsBlocked
        {
            get
            {
                return Role switch
                {
                    "Student" => Student?.IsBlocked ?? false,
                    "Tutor" => Tutor?.IsBlocked ?? false,
                    "Admin" => false,
                    _ => false
                };
            }
        }
        
        
    }
}