namespace TutorConnectAPI.Models
{
    public class Rating
    {
        public int Id { get; set; }
        public int TutorId { get; set; }
        public int StudentId { get; set; }
        public int Stars { get; set; }  // From 1 to 5
        public string Comment { get; set; }
        public DateTime DateRated { get; set; } = DateTime.UtcNow;

        public Tutor Tutor { get; set; }
        public Student Student { get; set; }
    }

}
