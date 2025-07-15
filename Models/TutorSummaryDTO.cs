namespace TutorConnect.WebApp.Models
{
    public class TutorSummaryDTO
    {
        public int TutorId { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public string Bio { get; set; }
        public List<ModuleDTO> Modules { get; set; }
    }
}
