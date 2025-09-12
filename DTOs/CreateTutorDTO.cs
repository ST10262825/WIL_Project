namespace TutorConnectAPI.DTOs
{
    public class CreateTutorDTO
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public string Phone { get; set; }
        public string Bio { get; set; }
        public List<int> ModuleIds { get; set; }
    }
}