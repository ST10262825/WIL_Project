namespace TutorConnect.WebApp.Models
{
    public class UpdateTutorDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public string Phone { get; set; }
        public string Bio { get; set; }
        public List<int> ModuleIds { get; set; }
        public bool IsBlocked { get; set; }
    }


}
