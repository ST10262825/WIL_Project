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

        public DateTime SessionDate { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = "Pending"; // Pending, Accepted, Declined, Completed
    }
}
