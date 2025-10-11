namespace TutorConnect.WebApp.Models
{
    public class ChatQuestionRequest
        {
            public string Question { get; set; }
            public int? ConversationId { get; set; }
            public string Context { get; set; } // Additional context about user's current page/action
        public int? UserId { get; set; }
    }

        public class ChatResponse
        {
            public string Answer { get; set; }
            public int ConversationId { get; set; }
            public int MessageId { get; set; }
            public List<QuickSuggestion> Suggestions { get; set; } = new();
            public List<RelevantDocument> RelevantDocs { get; set; } = new();
            public bool RequiresHumanSupport { get; set; }
            public string SupportCategory { get; set; }
        }

        public class QuickSuggestion
        {
            public string Text { get; set; }
            public string Type { get; set; } // "question", "action", "link"
            public string Action { get; set; } // URL or action identifier
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
            public List<ChatbotMessageDTO> Messages { get; set; } = new();
        }

        public class ChatbotMessageDTO
    {
            public string Content { get; set; }
            public bool IsUserMessage { get; set; }
            public DateTime SentAt { get; set; }
            public string MessageType { get; set; }
        }
    }

