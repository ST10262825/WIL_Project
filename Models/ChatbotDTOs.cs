namespace TutorConnect.WebApp.Models
{
    public class ChatQuestionRequest
    {
        public string Question { get; set; }
        public int? ConversationId { get; set; }
        public string Context { get; set; }
    }

    public class ChatResponse
    {
        public string Answer { get; set; }
        public List<QuickSuggestion> Suggestions { get; set; } = new List<QuickSuggestion>();
        public List<RelevantDocument> RelevantDocs { get; set; } = new List<RelevantDocument>();
        public bool RequiresHumanSupport { get; set; }
        public string SupportCategory { get; set; }
        public int? ConversationId { get; set; }
        public int? MessageId { get; set; }
    }

    public class QuickSuggestion
    {
        public string Text { get; set; }
        public string Type { get; set; } // "question" or "action"
        public string Action { get; set; } // URL or action to perform
    }

    public class RelevantDocument
    {
        public string Title { get; set; }
        public string Snippet { get; set; }
        public string Url { get; set; }
        public float RelevanceScore { get; set; }
    }

    public class ConversationDTO
    {
        public int ConversationId { get; set; }
        public string Title { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<ChatbotMessageDTO> Messages { get; set; } = new List<ChatbotMessageDTO>();
    }

    public class ChatbotMessageDTO
    {
        public string Content { get; set; }
        public bool IsUserMessage { get; set; }
        public DateTime SentAt { get; set; }
        public string MessageType { get; set; }
    }
}

