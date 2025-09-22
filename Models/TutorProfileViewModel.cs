using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewEngines;
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

        // Optional: Legacy single-line expertise field
        [Display(Name = "Expertise (comma separated)")]
        [StringLength(500, ErrorMessage = "Expertise cannot exceed 500 characters.")]
        public string? Expertise { get; set; }

        // Optional: Legacy single-line education field
        [Display(Name = "Education & Qualifications")]
        [StringLength(2000, ErrorMessage = "Education & Qualifications cannot exceed 2000 characters.")]
        public string? Education { get; set; }

        // New: multiple expertise entries
        public List<string> ExpertiseList { get; set; } = new();

        // New: structured education entries
        public List<EducationDTO> EducationList { get; set; } = new();

        //Rating properties
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public int RatingCount1 { get; set; }
        public int RatingCount2 { get; set; }
        public int RatingCount3 { get; set; }
        public int RatingCount4 { get; set; }
        public int RatingCount5 { get; set; }
        public List<ReviewDTO> Reviews { get; set; } = new List<ReviewDTO>();
        // Availability properties
        public List<TimeSlotDTO> Availability { get; set; } = new();

    }

    public class EducationDTO
    {
        [Display(Name = "Qualification")]
        public string Qualification { get; set; } = string.Empty;

        [Display(Name = "School / University")]
        public string School { get; set; } = string.Empty;

        [Display(Name = "Graduation Year")]
        public string Year { get; set; } = string.Empty;
    }

}
