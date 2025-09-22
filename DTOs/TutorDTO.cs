using TutorConnectAPI.Models;

namespace TutorConnectAPI.DTOs
{
    public class TutorDTO
    {
        public int TutorId { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }

        public string Email { get; set; }
        public string Phone { get; set; }
        public string? ProfileImageUrl { get; set; }
        public string Bio { get; set; }
        public bool IsBlocked { get; set; }

        public string? AboutMe { get; set; }
        public string? Expertise { get; set; }
        public string? Education { get; set; }

        //Review and Rating Properties
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public int RatingCount1 { get; set; }
        public int RatingCount2 { get; set; }
        public int RatingCount3 { get; set; }
        public int RatingCount4 { get; set; }
        public int RatingCount5 { get; set; }
        public int TotalBookings { get; set; }
        public List<ModuleDTO> Modules { get; set; }
    }

}
