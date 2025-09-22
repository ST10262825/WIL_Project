namespace TutorConnect.WebApp.Models
{
    public class BrowseTutorDTO
    {
        public int TutorId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string ProfileImageUrl { get; set; } = "/images/default-profile.png";
        public string AboutMe { get; set; } = string.Empty;
        public string Expertise { get; set; } = string.Empty;
        public string Education { get; set; } = string.Empty;

        public List<string> Subjects { get; set; } = new();
        public bool IsVerified { get; set; } = true;

        // Add rating properties
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public int RatingCount1 { get; set; }
        public int RatingCount2 { get; set; }
        public int RatingCount3 { get; set; }
        public int RatingCount4 { get; set; }
        public int RatingCount5 { get; set; }
        public List<ReviewDTO> Reviews { get; set; } = new List<ReviewDTO>();
    }
}
