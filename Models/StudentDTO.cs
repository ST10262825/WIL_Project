namespace TutorConnect.WebApp.Models
{
    public class StudentDTO
    {
        public int StudentId { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; }
        public string Course { get; set; }
        public string? Bio { get; set; }
         public string? ProfileImage { get; set; }
 
        //public User User { get; set; }
    }
}
