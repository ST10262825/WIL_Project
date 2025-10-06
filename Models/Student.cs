using System.ComponentModel.DataAnnotations.Schema;

namespace TutorConnectAPI.Models
{
    public class Student
    {
        public int StudentId { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; }
        public string Course { get; set; }
        public string? ProfileImage { get; set; }
        public string? Bio { get; set; }
        public ICollection<Enrollment> Enrollments { get; set; }
        public ICollection<Session> Sessions { get; set; }
        public ICollection<Booking> Bookings { get; set; }
        public bool IsBlocked { get; set; } = false;
        [ForeignKey("Review")]
        public int? ReviewId { get; set; }  // nullable
        public Review? Review { get; set; }
        public User User { get; set; }
    }
}