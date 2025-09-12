namespace TutorConnectAPI.Models
{
    public class Enrollment
    {
        public int EnrollmentId { get; set; }
        public int StudentId { get; set; }
        public Student Student { get; set; }
        public int CourseId { get; set; }
        public Course Course { get; set; }
        public int CompletedSessions { get; set; }
        public int TotalSessions { get; set; }
    }
}
