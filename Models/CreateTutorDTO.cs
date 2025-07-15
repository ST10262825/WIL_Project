
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    namespace TutorConnect.WebApp.Models
    {
        public class CreateTutorDTO
        {
            [Required]
            public string Email { get; set; }

            [Required]
            [MinLength(6)]
            public string Password { get; set; }

            [Required]
            public string Name { get; set; }

            public string Surname { get; set; }
            public string Phone { get; set; }
            public string Bio { get; set; }

            public List<int> ModuleIds { get; set; } = new();
        }
    }


