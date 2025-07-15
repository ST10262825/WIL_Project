namespace TutorConnectAPI.DTOs
{
   
        public class UpdateTutorDTO
        {
            public string Name { get; set; }
            public string Surname { get; set; }
            public string Phone { get; set; }
            public string Bio { get; set; }
            public List<int> ModuleIds { get; set; }
            public bool IsBlocked { get; set; }
        }
    }


