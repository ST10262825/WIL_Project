using System.ComponentModel.DataAnnotations.Schema;

namespace TutorConnectAPI.Models
{
    public class Student
    {
        public int StudentId { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; }
        // Change from string Course to relationship
        public int CourseId { get; set; }
        public Course Course { get; set; }
        public string? ProfileImage { get; set; }
        public string? Bio { get; set; }
        public ICollection<Enrollment> Enrollments { get; set; }
        public ICollection<Session> Sessions { get; set; }
        public ICollection<Booking> Bookings { get; set; }
        public bool IsBlocked { get; set; } = false;
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public User User { get; set; }

        public ICollection<StudentMaterialAccess> MaterialAccesses { get; set; } = new List<StudentMaterialAccess>();
    }
}