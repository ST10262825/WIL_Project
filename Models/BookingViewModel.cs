using System.ComponentModel.DataAnnotations;

namespace TutorConnect.WebApp.Models
{
    public class BookingViewModel
    {
        [Required]
        public int TutorId { get; set; }

        [Required]
        public int StudentId { get; set; }

        [Required]
        public int ModuleId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime SelectedDate { get; set; }

        [Required]
        public DateTime StartTime { get; set; } // Will be set via JavaScript

        [Required]
        public DateTime EndTime { get; set; }   // Will be set via JavaScript

        public string? Notes { get; set; }
    }
}
