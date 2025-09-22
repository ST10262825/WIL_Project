using TutorConnect.WebApp.Services;

namespace TutorConnect.WebApp.Models
{
    public class AdminDashboardViewModel
    {
        public DashboardStatsDTO Stats { get; set; }
        public List<TutorDTO> Tutors { get; set; }
        public List<StudentDTO> Students { get; set; }
        public List<BookingDTO> Bookings { get; set; }
        public List<ModuleDTO> Modules { get; set; }
        public SystemHealthDTO SystemHealth { get; set; }
    }

    public class SystemHealthDTO
    {
        public string DatabaseStatus { get; set; }
        public long Uptime { get; set; }
        public long MemoryUsage { get; set; }
        public int ActiveConnections { get; set; }
    }
}