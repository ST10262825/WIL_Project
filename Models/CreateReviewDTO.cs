using System.ComponentModel.DataAnnotations;

namespace TutorConnect.WebApp.Models
{
    public class CreateReviewDTO
    {
        [Required]
        public int BookingId { get; set; }

        [Required]
        public int StudentId { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5 stars")]
        public int Rating { get; set; }

        [MaxLength(500, ErrorMessage = "Comment cannot exceed 500 characters")]
        public string? Comment { get; set; }
    }
}