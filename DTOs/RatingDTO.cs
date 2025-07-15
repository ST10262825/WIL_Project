namespace TutorConnectAPI.DTOs
{
    public class RatingDTO
    {
        public int TutorId { get; set; }
        public int StudentId { get; set; }
        public int Stars { get; set; }
        public string Comment { get; set; }
    }

}
