namespace TutorConnect.WebApp.Models
{
    public class ChatViewModel
    {
        public int CurrentUserId { get; set; }
        public int OtherUserId { get; set; }
        public string OtherUserName { get; set; }
        public string AuthToken { get; set; }
    }

}
