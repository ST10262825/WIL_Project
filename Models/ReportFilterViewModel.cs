namespace TutorConnect.WebApp.Models
{
    public class ReportFilterViewModel
    {
        public string ReportType { get; set; }
        public string DateRange { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string UserType { get; set; }
        public int? UserId { get; set; }
        public string ExportFormat { get; set; }

        // Individual properties for checkboxes
        public bool StatusPending { get; set; }
        public bool StatusConfirmed { get; set; }
        public bool StatusCompleted { get; set; }
        public bool StatusCancelled { get; set; }
        public bool StatusActive { get; set; }
        public bool StatusInactive { get; set; }

        // Module IDs as list
        public List<int> ModuleIds { get; set; } = new List<int>();

        // Helper methods to convert to DTO
        public ReportFilterDTO ToDto()
        {
            var statuses = new List<string>();
            if (StatusPending) statuses.Add("Pending");
            if (StatusConfirmed) statuses.Add("Confirmed");
            if (StatusCompleted) statuses.Add("Completed");
            if (StatusCancelled) statuses.Add("Cancelled");
            if (StatusActive) statuses.Add("Active");
            if (StatusInactive) statuses.Add("Inactive");

            return new ReportFilterDTO
            {
                ReportType = ReportType,
                DateRange = DateRange,
                StartDate = StartDate,
                EndDate = EndDate,
                UserType = UserType,
                UserId = UserId,
                ExportFormat = ExportFormat,
                Statuses = statuses,
                ModuleIds = ModuleIds
            };
        }
    }
}
