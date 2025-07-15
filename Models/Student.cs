namespace TutorConnectAPI.Models
{
    public class Student
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; }
        public string Course { get; set; }
        public User User { get; set; }
    }
}