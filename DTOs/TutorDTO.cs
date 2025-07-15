namespace TutorConnectAPI.DTOs
{
    public class TutorDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public string Phone { get; set; }
        public string Bio { get; set; }
        public bool IsBlocked { get; set; }


        public List<ModuleDTO> Modules { get; set; }
    }

}
