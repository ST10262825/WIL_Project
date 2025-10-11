using System.ComponentModel.DataAnnotations;

namespace TutorConnectAPI.Models
{
    public class ChatbotConversation
    {
        [Key]
        public int ConversationId { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
        public string Title { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public List<ChatbotMessage> Messages { get; set; } = new();
        public bool IsActive { get; set; } = true;
    }

    public class ChatbotMessage
    {
        [Key]
        public int MessageId { get; set; }
        public int ConversationId { get; set; }
        public ChatbotConversation Conversation { get; set; }
        public string Content { get; set; }
        public bool IsUserMessage { get; set; }
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public string? MessageType { get; set; } = "text"; // "text", "suggestion", "action"
        public string? Metadata { get; set; } // JSON for additional data
    }

    public class KnowledgeBaseDocument
    {
        [Key]
        public int DocumentId { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string? DocumentType { get; set; } // "feature", "faq", "tutorial", "policy"
        public string? Category { get; set; }
        public List<string>? Tags { get; set; } = new();
        public float[]? Embedding { get; set; } // Vector embedding for similarity search
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }

    public class ChatbotSuggestion
    {
        [Key]
        public int SuggestionId { get; set; }
        public string Question { get; set; }
        public string Category { get; set; }
        public int UsageCount { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
