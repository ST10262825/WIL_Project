using System.ComponentModel.DataAnnotations;
using TutorConnect.WebApp.Models;

public class RegisterStudentDTO
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public int CourseId { get; set; }

    // POPIA Compliance Fields - FIXED VALIDATION
    [Required(ErrorMessage = "You must accept the terms and conditions")]
    [MustBeTrue(ErrorMessage = "You must accept the terms and conditions")]
    public bool HasAcceptedPOPIA { get; set; }

    public bool MarketingConsent { get; set; }

}
