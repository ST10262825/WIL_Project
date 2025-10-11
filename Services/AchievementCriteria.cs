namespace TutorConnectAPI.Services
{
    public class AchievementCriteria
    {
        public string Type { get; set; } = string.Empty;
        public int Required { get; set; }
        public string? AdditionalData { get; set; }
    }
}
