using System.Collections.Generic;

namespace TutorConnect.WebApp.Models
{
    public class BrowseTutorsViewModel
    {
        public int TutorId { get; set; }
        public string Name { get; set; } = "";
        public string Surname { get; set; } = "";
        public string ProfileImageUrl { get; set; } = "";
        public string AboutMe { get; set; } = "";
        public List<string> Subjects { get; set; } = new();
        public List<ModuleDTO> Modules { get; set; } = new();
    }
}
