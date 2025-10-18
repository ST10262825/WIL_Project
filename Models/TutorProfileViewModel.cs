using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using System.ComponentModel.DataAnnotations;

namespace TutorConnect.WebApp.Models
{
    public class TutorProfileViewModel
    {
        public int TutorId { get; set; }

        public string Name { get; set; } = string.Empty;

        [Display(Name = "Bio")]
        [StringLength(500, ErrorMessage = "Bio cannot exceed 500 characters.")]
        public string? Bio { get; set; }

        [Display(Name = "Profile Image")]
        public IFormFile? ProfileImage { get; set; }

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

        public List<string> ExpertiseList { get; set; } = new();

        public List<EducationDTO> EducationList { get; set; } = new();

        // ✅ FIX: Make these nullable or provide defaults
        public int? CourseId { get; set; }  // Changed to nullable
        public string? CourseName { get; set; }  // Changed to nullable

        // Rating properties - these aren't in the form either
        public double AverageRating { get; set; } = 0;  // Default value
        public int TotalReviews { get; set; } = 0;
        public int RatingCount1 { get; set; } = 0;
        public int RatingCount2 { get; set; } = 0;
        public int RatingCount3 { get; set; } = 0;
        public int RatingCount4 { get; set; } = 0;
        public int RatingCount5 { get; set; } = 0;

        public List<ReviewDTO> Reviews { get; set; } = new List<ReviewDTO>();

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