// DTOs/AccountSecurityDTOs.cs
using System.ComponentModel.DataAnnotations;

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

    public class ForgotPasswordDTO
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }

    public class ResetPasswordDTO
    {
        [Required]
        public string Token { get; set; }

        [Required]
        [MinLength(6)]
        public string NewPassword { get; set; }

        [Required]
        [Compare("NewPassword")]
        public string ConfirmPassword { get; set; }
    }

    public class ValidateResetTokenDTO
    {
        [Required]
        public string Token { get; set; }
    }
}