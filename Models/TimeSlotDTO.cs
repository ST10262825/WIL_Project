namespace TutorConnect.WebApp.Models
{
    public class TimeSlotDTO
    {
        public DateTime Start { get; set; }   // Must match API property name "Start"
        public DateTime End { get; set; }     // Must match API property name "End"
        public bool Available { get; set; }   // Must match API property name "Available"
    }
}