using System.ComponentModel.DataAnnotations;

namespace TutorConnect.WebApp.Models
{
    public class TutorProfileViewModel
    {
        public int TutorId { get; set; }

        // Read-only: Tutor full name
        public string Name { get; set; } = string.Empty;

        // Editable bio
        [Display(Name = "Bio")]
        [StringLength(500, ErrorMessage = "Bio cannot exceed 500 characters.")]
        public string? Bio { get; set; }

        // Editable profile image
        [Display(Name = "Profile Image")]
        public IFormFile? ProfileImage { get; set; }

        // URL of the current profile image (used to display existing image)
        public string? ProfileImageUrl { get; set; }

        [Display(Name = "About Me")]
        [StringLength(2000, ErrorMessage = "About Me cannot exceed 2000 characters.")]
        public string? AboutMe { get; set; }

        [Display(Name = "Expertise (comma separated)")]
        [StringLength(500, ErrorMessage = "Expertise cannot exceed 500 characters.")]
        public string? Expertise { get; set; }

        [Display(Name = "Education & Qualifications")]
        [StringLength(2000, ErrorMessage = "Education & Qualifications cannot exceed 2000 characters.")]
        public string? Education { get; set; }
    }
}
