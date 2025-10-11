using System.ComponentModel.DataAnnotations;


namespace TutorConnectAPI.Models
{
    public class StudentMaterialAccess
    {
        [Key]
        public int AccessId { get; set; }
        public int StudentId { get; set; }
        public int LearningMaterialId { get; set; }
        public int? BookingId { get; set; } // Which booking granted access
        public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiresAt { get; set; } // Optional expiration

        // Navigation properties
        public Student Student { get; set; }
        public LearningMaterial LearningMaterial { get; set; }
        public Booking Booking { get; set; }
    }
}
