using TutorConnect.WebApp.Services;

namespace TutorConnect.WebApp.Models
{
    public class AdminDashboardViewModel
    {
        // Basic counts
        public int TotalTutors { get; set; }
        public int TotalStudents { get; set; }
        public int TotalBookings { get; set; }
        public int TotalModules { get; set; }
        public int TotalCourses { get; set; }

        // Status breakdowns
        public int ActiveTutors { get; set; }
        public int BlockedTutors { get; set; }
        public int ActiveStudents { get; set; }
        public int BlockedStudents { get; set; }

        // Booking statuses
        public int PendingBookings { get; set; }
        public int ConfirmedBookings { get; set; }
        public int CompletedBookings { get; set; }
        public int CancelledBookings { get; set; }

        // Financial
        public decimal TotalRevenue { get; set; }

        // Recent activity
        public int RecentBookings { get; set; }
        public int CompletedThisWeek { get; set; }

        // Performance metrics
        public double AverageTutorRating { get; set; }
        public decimal BookingCompletionRate { get; set; }

        // Calculated property
        public int ActiveUsers => ActiveTutors + ActiveStudents;

        // Lists
        public List<ModuleDTO> PopularModules { get; set; } = new List<ModuleDTO>();
        public List<TutorDTO> TopRatedTutors { get; set; } = new List<TutorDTO>();
        public List<StudentDTO> Students { get; set; } = new List<StudentDTO>();
        public List<BookingDTO> Bookings { get; set; } = new List<BookingDTO>();
        public List<ModuleDTO> Modules { get; set; } = new List<ModuleDTO>();

        public SystemHealthDTO SystemHealth { get; set; } = new SystemHealthDTO();
    }

    public class SystemHealthDTO
    {
        public string DatabaseStatus { get; set; } = "Online";
        public long Uptime { get; set; }
        public long MemoryUsage { get; set; }
        public int ActiveConnections { get; set; }
    }
}