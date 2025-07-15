using System.Reflection;

namespace TutorConnect.WebApp.Models
{

    public class TutorModuleDTO
    {
        public int TutorId { get; set; }
        public int ModuleId { get; set; }

        // Optional: Just include names if needed
        public string ModuleName { get; set; }
        public string ModuleCode { get; set; }
    }

}
