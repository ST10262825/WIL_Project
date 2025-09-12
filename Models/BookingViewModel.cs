using System.ComponentModel.DataAnnotations;

namespace TutorConnect.WebApp.Models
{
    public class BookingViewModel
    {
        [Required]
        public int TutorId { get; set; }

        [Required]
        public int StudentId { get; set; } // we’ll pull this from the logged-in student session

        [Required]
        public int ModuleId { get; set; }

        [Required(ErrorMessage = "Please select a session date.")]
        [DataType(DataType.DateTime)]
        public DateTime SessionDate { get; set; }


        public string? Notes { get; set; }
    }
}
