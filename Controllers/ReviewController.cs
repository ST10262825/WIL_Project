using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TutorConnectAPI.Data;
using TutorConnectAPI.DTOs;
using TutorConnectAPI.Models;

namespace TutorConnectAPI.Controllers
{
    [ApiController]
    [Route("api/reviews")]
    public class ReviewController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ReviewController(ApplicationDbContext context)
        {
            _context = context;
        }

        // POST: api/reviews
        [HttpPost]
        public async Task<IActionResult> CreateReview([FromBody] CreateReviewDTO dto)
        {
            try
            {
                // Check if review already exists for this booking
                var existingReview = await _context.Reviews
                    .FirstOrDefaultAsync(r => r.BookingId == dto.BookingId);

                if (existingReview != null)
                {
                    return BadRequest("Review already exists for this booking");
                }

                // Verify the booking exists and is completed
                var booking = await _context.Bookings
                    .Include(b => b.Tutor)
                    .FirstOrDefaultAsync(b => b.BookingId == dto.BookingId && b.Status == "Completed");

                if (booking == null)
                {
                    return BadRequest("Invalid booking or booking not completed");
                }

                var review = new Review
                {
                    BookingId = dto.BookingId,
                    TutorId = booking.TutorId,
                    StudentId = dto.StudentId,
                    Rating = dto.Rating,
                    Comment = dto.Comment,
                    IsVerified = true
                };

                _context.Reviews.Add(review);

                // Update tutor ratings using Bayesian average
                await UpdateTutorRatings(booking.TutorId, dto.Rating);

                await _context.SaveChangesAsync();

                return Ok(new { message = "Review submitted successfully", reviewId = review.ReviewId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while creating review");
            }
        }

        // GET: api/reviews/tutor/{tutorId}
        [HttpGet("tutor/{tutorId}")]
        public async Task<IActionResult> GetTutorReviews(int tutorId)
        {
            try
            {
                var reviews = await _context.Reviews
                    .Include(r => r.Student)
                    .Where(r => r.TutorId == tutorId && r.IsVerified)
                    .OrderByDescending(r => r.CreatedDate)
                    .Select(r => new ReviewDTO
                    {
                        ReviewId = r.ReviewId,
                        BookingId = r.BookingId,
                        StudentName = r.Student.Name,
                        Rating = r.Rating,
                        Comment = r.Comment,
                        CreatedDate = r.CreatedDate,
                        IsVerified = r.IsVerified
                    })
                    .ToListAsync();

                return Ok(reviews);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while fetching reviews");
            }
        }

        // GET: api/reviews/student/{studentId}/pending
        [HttpGet("student/{studentId}/pending")]
        public async Task<IActionResult> GetPendingReviews(int studentId)
        {
            try
            {
                var pendingReviews = await _context.Bookings
                    .Include(b => b.Tutor)
                    .Include(b => b.Module)
                    .Where(b => b.StudentId == studentId &&
                               b.Status == "Completed" &&
                               !_context.Reviews.Any(r => r.BookingId == b.BookingId))
                    .Select(b => new PendingReviewDTO
                    {
                        BookingId = b.BookingId,
                        TutorName = b.Tutor.Name + " " + b.Tutor.Surname,
                        TutorId = b.TutorId,
                        ModuleName = b.Module.Name,
                        SessionDate = b.StartTime,
                        TutorProfileImageUrl = b.Tutor.ProfileImageUrl
                    })
                    .ToListAsync();

                return Ok(pendingReviews);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while fetching pending reviews");
            }
        }

        private async Task UpdateTutorRatings(int tutorId, int newRating)
        {
            var tutor = await _context.Tutors.FindAsync(tutorId);
            if (tutor == null) return;

            // Update individual rating counts
            switch (newRating)
            {
                case 1: tutor.RatingCount1++; break;
                case 2: tutor.RatingCount2++; break;
                case 3: tutor.RatingCount3++; break;
                case 4: tutor.RatingCount4++; break;
                case 5: tutor.RatingCount5++; break;
            }

            tutor.TotalReviews = tutor.RatingCount1 + tutor.RatingCount2 + tutor.RatingCount3 +
                                tutor.RatingCount4 + tutor.RatingCount5;

            // Calculate Bayesian average (like IMDB/Google Play)
            const int confidence = 5; // Confidence factor
            const double expectedAverage = 3.5; // Expected average rating

            double bayesianAverage = (confidence * expectedAverage +
                                    (tutor.RatingCount1 * 1 + tutor.RatingCount2 * 2 +
                                     tutor.RatingCount3 * 3 + tutor.RatingCount4 * 4 +
                                     tutor.RatingCount5 * 5)) /
                                    (confidence + tutor.TotalReviews);

            tutor.AverageRating = Math.Round(bayesianAverage, 1);

            // ✅ CRITICAL: Save the changes to the database
            _context.Tutors.Update(tutor);
            await _context.SaveChangesAsync(); // This line was missing!
        }

        // GET: api/reviews/booking/{bookingId}/reviewed
        [HttpGet("booking/{bookingId}/reviewed")]
        public async Task<IActionResult> HasBookingBeenReviewed(int bookingId)
        {
            try
            {
                var hasBeenReviewed = await _context.Reviews
                    .AnyAsync(r => r.BookingId == bookingId && r.IsVerified);

                return Ok(new { hasBeenReviewed });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while checking review status");
            }
        }
    }
}