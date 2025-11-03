// DTOs/ThemePreferenceDTO.cs
namespace TutorConnectAPI.DTOs
{
    public class ThemePreferenceDTO
    {
        public string Theme { get; set; }
    }

    public class UserPreferencesDTO
    {
        public string Theme { get; set; }
        public DateTime? LastUpdated { get; set; }
    }
}