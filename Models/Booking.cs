using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TutorConnectAPI.Models
{
    public class Booking
    {
        [Key]
        public int BookingId { get; set; }

        [ForeignKey("Tutor")]
        public int TutorId { get; set; }
        public Tutor Tutor { get; set; }

        [ForeignKey("Student")]
        public int StudentId { get; set; }
        public Student Student { get; set; }

        [ForeignKey("Module")]
        public int ModuleId { get; set; }
        public Module Module { get; set; }

        [ForeignKey("Review")]
        public int? ReviewId { get; set; }  // nullable
        public Review? Review { get; set; }


        [Required]
        public DateTime StartTime { get; set; }  // e.g., 2025-09-20 10:00

        [Required]
        public DateTime EndTime { get; set; }    // e.g., 2025-09-20 11:00

        [MaxLength(500)]
        public string? Notes { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = "Pending"; // Pending, Accepted, Declined, Completed
        public DateTime? CompletedAt { get; set; }
    }
}
