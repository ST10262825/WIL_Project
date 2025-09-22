using System.ComponentModel.DataAnnotations;

namespace TutorConnect.WebApp.Models
{
    public class CreateModuleViewModel
    {
        [Required]
        public string Code { get; set; }

        [Required]
        public string Name { get; set; }
    }
}
