namespace TutorConnect.WebApp.Models
{
    public class TutorRatingDTO
    {
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public int RatingCount1 { get; set; }
        public int RatingCount2 { get; set; }
        public int RatingCount3 { get; set; }
        public int RatingCount4 { get; set; }
        public int RatingCount5 { get; set; }
        public string RatingDistribution { get; set; } = string.Empty;
    }
}