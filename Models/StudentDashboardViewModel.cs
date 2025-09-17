namespace TutorConnect.WebApp.Models
{
    public class StudentDashboardViewModel
    {
        public StudentDTO Student { get; set; }
        public List<BookingDTO> UpcomingSessions { get; set; }
        public List<CourseProgressDTO> Progress { get; set; }
        public List<MessageDTO> RecentMessages { get; set; }
        public List<TutorDTO> Tutors { get; set; }
    }

}
