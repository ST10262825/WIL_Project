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


        // POPIA Compliance Fields
        public bool HasAcceptedPOPIA { get; set; } = false;
        public DateTime? POPIAAcceptedDate { get; set; }
        public string? POPIAVersion { get; set; } // Track which version of terms they accepted
        public bool MarketingConsent { get; set; } = false;
        public DateTime? LastConsentUpdate { get; set; }

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