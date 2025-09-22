namespace TutorConnectAPI.Models
{
    public class Tutor
    {
        public int TutorId { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public string Phone { get; set; }
        public string Bio { get; set; }
        public string? ProfileImageUrl { get; set; }
        public string? AboutMe { get; set; }
        public string? Expertise { get; set; }
        public string? Education { get; set; }
        public bool IsBlocked { get; set; } = false;
       
        public ICollection<Session> Sessions { get; set; }
        public ICollection<Booking> Bookings { get; set; }
        public ICollection<TutorModule> TutorModules { get; set; }
      
        public User User { get; set; }

    //Review and Rating Properties
        public double AverageRating { get; set; } = 0;
        public int TotalReviews { get; set; } = 0;
        public int RatingCount1 { get; set; } = 0;
        public int RatingCount2 { get; set; } = 0;
        public int RatingCount3 { get; set; } = 0;
        public int RatingCount4 { get; set; } = 0;
        public int RatingCount5 { get; set; } = 0;

        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}