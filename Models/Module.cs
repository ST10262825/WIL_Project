namespace TutorConnectAPI.Models
{
    public class Module
    {
        public int ModuleId { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public ICollection<Session> Sessions { get; set; }
        public ICollection<TutorModule> TutorModules { get; set; }

    }
}