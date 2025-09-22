using System.ComponentModel.DataAnnotations;

namespace TutorConnectAPI.DTOs
{
    public class AdminUpdateTutorDTO
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(50, ErrorMessage = "Name cannot exceed 50 characters")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Surname is required")]
        [StringLength(50, ErrorMessage = "Surname cannot exceed 50 characters")]
        public string Surname { get; set; }

        [Phone(ErrorMessage = "Please enter a valid phone number")]
        public string Phone { get; set; }

        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Bio is required")]
        [StringLength(500, ErrorMessage = "Bio cannot exceed 500 characters")]
        public string Bio { get; set; }

        [StringLength(1000, ErrorMessage = "About Me cannot exceed 1000 characters")]
        public string AboutMe { get; set; }

        [StringLength(200, ErrorMessage = "Expertise cannot exceed 200 characters")]
        public string Expertise { get; set; }

        [StringLength(200, ErrorMessage = "Education cannot exceed 200 characters")]
        public string Education { get; set; }

        public bool IsBlocked { get; set; }

        [Required(ErrorMessage = "At least one module must be selected")]
        [MinLength(1, ErrorMessage = "At least one module must be selected")]
        public List<int> ModuleIds { get; set; } = new List<int>();
    }
}
