// DTOs/AccountSecurityDTOs.cs
namespace TutorConnectAPI.DTOs
{
    public class ChangePasswordDTO
    {
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmPassword { get; set; }
    }

    public class DeleteAccountDTO
    {
        public string Password { get; set; }
        public string Confirmation { get; set; } // User must type "DELETE" to confirm
    }

    public class ThemePreferenceDTO
    {
        public string Theme { get; set; } // "light" or "dark"
    }
}