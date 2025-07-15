namespace TutorConnectAPI.Models
{
    public class Tutor
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public string Phone { get; set; }
        public string Bio { get; set; }
        //public string? ProfilePicUrl { get; set; }
        public bool IsBlocked { get; set; } = false;

        public ICollection<TutorModule> TutorModules { get; set; }
        public ICollection<Rating> Ratings { get; set; }
        public User User { get; set; }
    }
}