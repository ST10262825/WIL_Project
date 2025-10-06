namespace TutorConnectAPI.DTOs
{
    public class EverythingReportDTO
    {
        public StudentReportDTO? StudentReport { get; set; }
        public TutorReportDTO? TutorReport { get; set; }
        public BookingReportDTO? BookingReport { get; set; }
        public ModuleDemandReportDTO? ModuleDemandReport { get; set; }
        public OverallStatsDTO OverallStats { get; set; } = new OverallStatsDTO();
    }
}
