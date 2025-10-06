namespace TutorConnectAPI.DTOs
{
    // ReportFilterDTO.cs
    public class ReportFilterDTO
    {
        public string ReportType { get; set; } // "Student", "Tutor", "Booking", "Financial", "ModuleDemand"
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string DateRange { get; set; } // "last7days", "thismonth", "lastmonth", "custom"
        public int? UserId { get; set; }
        public string? UserType { get; set; } // "Tutor", "Student"
        public List<string> Statuses { get; set; } = new List<string>();
        public List<int> ModuleIds { get; set; } = new List<int>();
        public string? ExportFormat { get; set; } // "pdf", "excel", "csv"
    }

    // ReportResultDTO.cs
    public class ReportResultDTO
    {
        public string ReportTitle { get; set; }
        public DateTime GeneratedAt { get; set; }
        public ReportFilterDTO Filters { get; set; }
        public object Data { get; set; }
        public ChartDataDTO Charts { get; set; }
        public SummaryDTO Summary { get; set; }
    }

    public class ChartDataDTO
    {
        public List<ChartSeriesDTO> Series { get; set; }
        public List<string> Labels { get; set; }
    }

    public class ChartSeriesDTO
    {
        public string Name { get; set; }
        public List<decimal> Data { get; set; }
    }

    public class SummaryDTO
    {
        public int TotalRecords { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageRating { get; set; }
        public int ActiveUsers { get; set; }
        // Add other summary metrics as needed
    }

    // Specific Report DTOs
    public class StudentReportDTO
    {
        public List<StudentReportItemDTO> Students { get; set; }
        public int TotalRegistrations { get; set; }
        public int TotalBookings { get; set; }
        public int TotalCancellations { get; set; }
        public decimal AverageSessionsPerStudent { get; set; }
    }

    public class StudentReportItemDTO
    {
        public int StudentId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public DateTime RegistrationDate { get; set; }
        public int TotalBookings { get; set; }
        public int CompletedSessions { get; set; }
        public int CancelledSessions { get; set; }
        public decimal EngagementScore { get; set; }
        public bool IsActive { get; set; }
    }

    public class TutorReportDTO
    {
        public List<TutorReportItemDTO> Tutors { get; set; }
        public int ActiveTutors { get; set; }
        public decimal AverageRating { get; set; }
        public int TotalCompletedSessions { get; set; }
        public decimal TotalRevenueGenerated { get; set; }
    }

    public class TutorReportItemDTO
    {
        public int TutorId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public List<string> Modules { get; set; }
        public double AverageRating { get; set; }
        public int TotalSessions { get; set; }
        public int CompletedSessions { get; set; }
        public decimal TotalEarnings { get; set; }
        public bool IsActive { get; set; }
        public DateTime JoinDate { get; set; }
    }

    public class BookingReportDTO
    {
        public List<BookingReportItemDTO> Bookings { get; set; }
        public int PendingCount { get; set; }
        public int ConfirmedCount { get; set; }
        public int CompletedCount { get; set; }
        public int CancelledCount { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class BookingReportItemDTO
    {
        public int BookingId { get; set; }
        public string StudentName { get; set; }
        public string TutorName { get; set; }
        public string Module { get; set; }
        public DateTime SessionDate { get; set; }
        public string Status { get; set; }
        public decimal Amount { get; set; }
        public int Duration { get; set; }
    }

   

    public class ModuleDemandReportDTO
    {
        public List<ModuleDemandItemDTO> Modules { get; set; }
        public int TotalBookings { get; set; }
        public string MostPopularModule { get; set; }
        public string LeastPopularModule { get; set; }
    }

    public class ModuleDemandItemDTO
    {
        public int ModuleId { get; set; }
        public string ModuleName { get; set; }
        public string ModuleCode { get; set; }
        public int TotalBookings { get; set; }
        public int UniqueTutors { get; set; }
        public int UniqueStudents { get; set; }
        public decimal TotalRevenue { get; set; }
        public double GrowthRate { get; set; }
    }
}
