using System.ComponentModel.DataAnnotations;

namespace TutorConnectAPI.DTOs
{
    public class CreateReviewDTO
    {
        public int BookingId { get; set; }
        public int StudentId { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }

        [MaxLength(500)]
        public string? Comment { get; set; }
    }

    public class ReviewDTO
    {
        public int ReviewId { get; set; }
        public int BookingId { get; set; }
        public string StudentName { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsVerified { get; set; }
    }

    public class PendingReviewDTO
    {
        public int BookingId { get; set; }
        public int TutorId { get; set; }
        public string TutorName { get; set; }
        public string ModuleName { get; set; }
        public DateTime SessionDate { get; set; }
        public string? TutorProfileImageUrl { get; set; }
    }

    public class TutorRatingDTO
    {
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public int RatingCount1 { get; set; }
        public int RatingCount2 { get; set; }
        public int RatingCount3 { get; set; }
        public int RatingCount4 { get; set; }
        public int RatingCount5 { get; set; }
        public string RatingDistribution { get; set; } // JSON string for chart data
    }
}