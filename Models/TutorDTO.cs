namespace TutorConnect.WebApp.Models
{
    public class TutorDTO
    {
        public int TutorId { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public string Phone { get; set; }
        public string? ProfileImageUrl { get; set; }
        public string Bio { get; set; }
        public string? AboutMe { get; set; }
        public string? Expertise { get; set; }
        public string? Education { get; set; }
        public bool IsBlocked { get; set; }
        public List<string> Subjects { get; set; } = new();
        public List<ModuleDTO> Modules { get; set; }
    }


}
