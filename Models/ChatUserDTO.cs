namespace TutorConnect.WebApp.Models
{
    public class ChatUserDTO
    {
        public int UserId { get; set; }
        public string Name { get; set; }
        public string Role { get; set; }
        public string Email { get; set; }
        public int UnreadCount { get; set; }
        public DateTime? LastMessageTime { get; set; }
        public string LastMessagePreview { get; set; }
        public bool IsOnline { get; set; }
    }
}