using Microsoft.EntityFrameworkCore;
using System.Text;
using TutorConnectAPI.Data;
using TutorConnectAPI.DTOs;
using TutorConnectAPI.Models;

namespace TutorConnectAPI.Services
{
    // Services/IChatbotService.cs
    public interface IChatbotService
    {
        Task<ChatResponse> ProcessQuestionAsync(ChatQuestionRequest request, int userId);
        Task<List<ConversationDTO>> GetUserConversationsAsync(int userId);
        Task<ConversationDTO> GetConversationAsync(int conversationId, int userId);
        Task<bool> DeleteConversationAsync(int conversationId, int userId);
        Task TrainKnowledgeBaseAsync(); // Initial training method
    }

    // Services/ChatbotService.cs
    public class ChatbotService : IChatbotService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ChatbotService> _logger;

        public ChatbotService(ApplicationDbContext context, IHttpClientFactory httpClientFactory,
                             IConfiguration configuration, ILogger<ChatbotService> logger)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<ChatResponse> ProcessQuestionAsync(ChatQuestionRequest request, int userId)
        {
            Console.WriteLine($"=== CHATBOT PROCESSING STARTED ===");
            Console.WriteLine($"User ID: {userId}, Question: {request.Question}");

            try
            {
                Console.WriteLine($"Step 1: Validating user {userId} exists...");

                // Validate user exists
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    Console.WriteLine($"❌ USER {userId} NOT FOUND IN DATABASE");
                    return new ChatResponse
                    {
                        Answer = "I apologize, but there seems to be an issue with your account. Please try logging out and back in.",
                        RequiresHumanSupport = true,
                        SupportCategory = "Account Issue"
                    };
                }
                Console.WriteLine($"✅ User {userId} exists: {user.Email}");

                Console.WriteLine($"Step 2: Getting or creating conversation...");

                // Get or create conversation
                var conversation = await GetOrCreateConversationAsync(request.ConversationId, userId, request.Question);
                Console.WriteLine($"✅ Conversation ID: {conversation.ConversationId}");

                Console.WriteLine($"Step 3: Saving user message...");

                // Save user message
                var userMessage = new ChatbotMessage
                {
                    ConversationId = conversation.ConversationId,
                    Content = request.Question,
                    IsUserMessage = true,
                    SentAt = DateTime.UtcNow
                };
                _context.ChatbotMessages.Add(userMessage);
                Console.WriteLine($"✅ User message created");

                Console.WriteLine($"Step 4: Searching knowledge base...");

                // Search relevant knowledge base documents
                List<RelevantDocument> relevantDocs;
                try
                {
                    relevantDocs = await SearchKnowledgeBaseAsync(request.Question);
                    Console.WriteLine($"✅ Found {relevantDocs.Count} relevant documents");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Knowledge base search failed: {ex.Message}");
                    relevantDocs = new List<RelevantDocument>();
                }

                Console.WriteLine($"Step 5: Generating AI response...");

                // Generate response using AI
                ChatResponse aiResponse;
                try
                {
                    aiResponse = await GenerateAIResponseAsync(request.Question, relevantDocs, request.Context, userId);
                    Console.WriteLine($"✅ AI response generated successfully");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ AI response generation failed: {ex.Message}");
                    aiResponse = new ChatResponse
                    {
                        Answer = await GenerateFallbackResponseAsync(request.Question),
                        Suggestions = new List<QuickSuggestion>(),
                        RelevantDocs = relevantDocs,
                        RequiresHumanSupport = false
                    };
                }

                Console.WriteLine($"Step 6: Saving AI response...");

                // Save AI response
                // Save AI response
                var aiMessage = new ChatbotMessage
                {
                    ConversationId = conversation.ConversationId,
                    Content = aiResponse.Answer,
                    IsUserMessage = false,
                    SentAt = DateTime.UtcNow,
                    MessageType = "text"
                };

                // Only set Metadata if we have data
                if (aiResponse.Suggestions?.Any() == true || aiResponse.RelevantDocs?.Any() == true)
                {
                    aiMessage.Metadata = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        Suggestions = aiResponse.Suggestions,
                        RelevantDocs = aiResponse.RelevantDocs
                    });
                }
                // Otherwise, leave it as null (which is now allowed)

                _context.ChatbotMessages.Add(aiMessage);
                Console.WriteLine($"✅ AI message added to context");

                Console.WriteLine($"Step 7: Updating conversation...");

                // Update conversation
                conversation.UpdatedAt = DateTime.UtcNow;
                conversation.Title = await GenerateConversationTitleAsync(conversation.ConversationId);
                Console.WriteLine($"✅ Conversation updated");

                Console.WriteLine($"Step 8: Saving changes to database...");

                await _context.SaveChangesAsync();
                Console.WriteLine($"✅ Database changes saved successfully");

                aiResponse.ConversationId = conversation.ConversationId;
                aiResponse.MessageId = aiMessage.MessageId;

                Console.WriteLine($"=== CHATBOT PROCESSING COMPLETED SUCCESSFULLY ===");
                return aiResponse;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌❌❌ CHATBOT PROCESSING FAILED ❌❌❌");
                Console.WriteLine($"Exception: {ex.GetType().Name}");
                Console.WriteLine($"Message: {ex.Message}");
                Console.WriteLine($"Stack: {ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.GetType().Name}");
                    Console.WriteLine($"Inner Message: {ex.InnerException.Message}");
                }

                return new ChatResponse
                {
                    Answer = "I apologize, but I'm having trouble processing your question right now. This might be a temporary issue. Please try again in a moment.",
                    RequiresHumanSupport = true,
                    SupportCategory = "Technical Issue"
                };
            }
        }


        private async Task<ChatbotConversation> GetOrCreateConversationAsync(int? conversationId, int userId, string firstQuestion)
        {
            _logger.LogInformation("GetOrCreateConversationAsync - ConversationId: {ConversationId}, UserId: {UserId}", conversationId, userId);

            if (conversationId.HasValue)
            {
                _logger.LogInformation("Looking for existing conversation: {ConversationId}", conversationId.Value);

                var existingConversation = await _context.ChatbotConversations
                    .FirstOrDefaultAsync(c => c.ConversationId == conversationId && c.UserId == userId);

                if (existingConversation != null)
                {
                    _logger.LogInformation("Found existing conversation: {ConversationId}", existingConversation.ConversationId);
                    return existingConversation;
                }
                else
                {
                    _logger.LogWarning("Conversation {ConversationId} not found for user {UserId}", conversationId.Value, userId);
                }
            }

            _logger.LogInformation("Creating new conversation for user {UserId}", userId);

            // Create new conversation
            var conversation = new ChatbotConversation
            {
                UserId = userId,
                Title = await GenerateInitialTitleAsync(firstQuestion),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.ChatbotConversations.Add(conversation);

            _logger.LogInformation("Saving new conversation to database...");
            await _context.SaveChangesAsync();
            _logger.LogInformation("New conversation created with ID: {ConversationId}", conversation.ConversationId);

            return conversation;
        }

        // Add this temporary debugging method to your ChatbotService
        private void LogDetailedError(Exception ex, string methodName)
        {
            _logger.LogError(ex, "Error in {MethodName}: {Message}\nStack: {StackTrace}",
                methodName, ex.Message, ex.StackTrace);

            if (ex.InnerException != null)
            {
                _logger.LogError("Inner exception: {InnerMessage}\nInner Stack: {InnerStackTrace}",
                    ex.InnerException.Message, ex.InnerException.StackTrace);
            }
        }

        
        private async Task<List<RelevantDocument>> SearchKnowledgeBaseAsync(string question)
        {
            // Simple keyword-based search (you can enhance with vector search later)
            var keywords = question.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var relevantDocs = await _context.KnowledgeBaseDocuments
                .Where(doc => doc.IsActive &&
                             keywords.Any(keyword =>
                                 doc.Content.ToLower().Contains(keyword) ||
                                 doc.Title.ToLower().Contains(keyword)))
                .Take(5)
                .Select(doc => new RelevantDocument
                {
                    Title = doc.Title,
                    Snippet = GetSnippet(doc.Content, keywords),
                    Url = $"/help/{doc.DocumentType}/{doc.DocumentId}",
                    RelevanceScore = CalculateRelevance(doc, keywords)
                })
                .OrderByDescending(doc => doc.RelevanceScore)
                .ToListAsync();

            return relevantDocs;
        }

        private async Task<ChatResponse> GenerateAIResponseAsync(string question, List<RelevantDocument> relevantDocs, string context, int userId)
        {
            // Use OpenAI API or Azure OpenAI to generate responses
            var httpClient = _httpClientFactory.CreateClient();

            // Build context from relevant documents
            var contextText = BuildContextFromDocuments(relevantDocs);

            // Get user context (role, current page, etc.)
            var userContext = await GetUserContextAsync(userId, context);

            var prompt = $"""
        You are TutorBot, an AI assistant for TutorConnect - a tutoring platform connecting students with tutors.

        USER CONTEXT:
        {userContext}

        RELEVANT KNOWLEDGE:
        {contextText}

        USER QUESTION: {question}

        Instructions:
        1. Answer based on the provided knowledge and TutorConnect platform specifics
        2. Be helpful, friendly, and concise
        3. If you don't know something, admit it and suggest contacting support
        4. Provide specific steps or links when possible
        5. Suggest related actions or questions that might be helpful

        Format your response in clear, readable markdown. Use bullet points for steps and **bold** for important terms.
        """;

            var aiResponse = await CallOpenAIAsync(prompt);

            return new ChatResponse
            {
                Answer = aiResponse,
                Suggestions = await GenerateSuggestionsAsync(question, userContext),
                RelevantDocs = relevantDocs.Take(3).ToList(),
                RequiresHumanSupport = ShouldEscalateToHuman(question, aiResponse)
            };
        }

        private async Task<string> CallOpenAIAsync(string prompt)
        {
            var apiKey = _configuration["OpenAI:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                // Fallback to rule-based responses if no API key
                return await GenerateFallbackResponseAsync(prompt);
            }

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            var requestBody = new
            {
                model = "gpt-3.5-turbo",
                messages = new[]
                {
                new { role = "system", content = "You are TutorBot, a helpful AI assistant for TutorConnect platform." },
                new { role = "user", content = prompt }
            },
                max_tokens = 500,
                temperature = 0.7
            };

            var response = await client.PostAsJsonAsync("https://api.openai.com/v1/chat/completions", requestBody);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<OpenAIResponse>();
                return result.choices[0].message.content;
            }

            return await GenerateFallbackResponseAsync(prompt);
        }




        private async Task<string> GenerateFallbackResponseAsync(string prompt)
        {
            try
            {
                // Extract the actual question from the prompt
                var question = ExtractUserQuestion(prompt);
                var lowerQuestion = question.ToLower();

                // Smart keyword matching with context awareness
                var response = await GenerateContextAwareResponse(question, lowerQuestion);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating fallback response");
                return GenerateGenericHelpResponse();
            }
        }

        private string ExtractUserQuestion(string prompt)
        {
            // Extract just the user's question from the full prompt
            var lines = prompt.Split('\n');
            var questionLine = lines.FirstOrDefault(line => line.StartsWith("USER QUESTION:"));
            if (questionLine != null)
            {
                return questionLine.Replace("USER QUESTION:", "").Trim();
            }

            // Fallback: return the last line that seems like a question
            return lines.Last().Trim();
        }

        private async Task<string> GenerateContextAwareResponse(string question, string lowerQuestion)
        {
            // Priority 1: Booking & Sessions (most common)
            if (ContainsAny(lowerQuestion, new[] { "book", "schedule", "session", "appointment", "meeting" }))
            {
                return await GenerateBookingResponse(question, lowerQuestion);
            }

            // Priority 2: Account & Profile
            if (ContainsAny(lowerQuestion, new[] { "account", "profile", "sign up", "register", "login", "password" }))
            {
                return GenerateAccountResponse(question, lowerQuestion);
            }

            // Priority 3: Payments & Pricing
            if (ContainsAny(lowerQuestion, new[] { "pay", "payment", "price", "cost", "fee", "refund", "money" }))
            {
                return GeneratePaymentResponse(question, lowerQuestion);
            }

            // Priority 4: Technical Issues
            if (ContainsAny(lowerQuestion, new[] { "error", "bug", "not working", "technical", "problem", "issue" }))
            {
                return GenerateTechnicalResponse(question, lowerQuestion);
            }

            // Priority 5: Tutor-specific questions
            if (ContainsAny(lowerQuestion, new[] { "become tutor", "tutor application", "teach", "tutor profile" }))
            {
                return GenerateTutorResponse(question, lowerQuestion);
            }

            // Priority 6: Platform Features
            if (ContainsAny(lowerQuestion, new[] { "feature", "how to", "what can", "can i", "is there" }))
            {
                return GenerateFeatureResponse(question, lowerQuestion);
            }

            // Default: Smart generic response
            return await GenerateSmartGenericResponse(question, lowerQuestion);
        }

        private async Task<string> GenerateBookingResponse(string question, string lowerQuestion)
        {
            var response = new StringBuilder();

            response.AppendLine("**📅 Booking Sessions on TutorConnect**");
            response.AppendLine();

            if (ContainsAny(lowerQuestion, new[] { "how", "steps", "process" }))
            {
                response.AppendLine("Here's how to book a session:");
                response.AppendLine("1. **Find Tutors**: Browse available tutors by subject or use the search feature");
                response.AppendLine("2. **View Profiles**: Check tutor ratings, reviews, and expertise areas");
                response.AppendLine("3. **Check Availability**: See real-time availability calendars");
                response.AppendLine("4. **Select Time**: Choose from available slots that work for you");
                response.AppendLine("5. **Confirm**: Review details and confirm your booking");
                response.AppendLine();
                response.AppendLine("💡 **Pro Tip**: Book in advance for popular tutors!");
            }

            if (ContainsAny(lowerQuestion, new[] { "cancel", "reschedule" }))
            {
                response.AppendLine("**🔄 Managing Your Sessions:**");
                response.AppendLine("- **Cancellation**: Cancel up to 24 hours before for full refund");
                response.AppendLine("- **Rescheduling**: Change time/date up to 12 hours before");
                response.AppendLine("- **Process**: Go to 'My Bookings' → Select session → Choose action");
                response.AppendLine();
                response.AppendLine("⚠️ Late cancellations may affect your rating");
            }

            if (ContainsAny(lowerQuestion, new[] { "find", "search", "available" }))
            {
                response.AppendLine("**🔍 Finding the Right Tutor:**");
                response.AppendLine("- **Search by**: Subject, module, tutor name, or rating");
                response.AppendLine("- **Filter by**: Availability, price range, or expertise");
                response.AppendLine("- **View**: Detailed profiles with student reviews");
                response.AppendLine();
                response.AppendLine("Try our **AI matching** feature for personalized recommendations!");
            }

            // Add dynamic suggestions based on time of day
            var currentHour = DateTime.UtcNow.Hour;
            if (currentHour >= 18 || currentHour <= 6)
            {
                response.AppendLine();
                response.AppendLine("🌙 *Evening tip: Many tutors have evening availability for busy students!*");
            }

            return response.ToString();
        }

        private string GenerateAccountResponse(string question, string lowerQuestion)
        {
            var response = new StringBuilder();

            response.AppendLine("**👤 Account Management**");
            response.AppendLine();

            if (ContainsAny(lowerQuestion, new[] { "create", "sign up", "register" }))
            {
                response.AppendLine("**Creating an Account:**");
                response.AppendLine("1. Click 'Sign Up' on the homepage");
                response.AppendLine("2. Choose 'Student' or 'Tutor' role");
                response.AppendLine("3. Fill in your details and verify email");
                response.AppendLine("4. Complete your profile for better matching");
                response.AppendLine();
                response.AppendLine("🎉 **Welcome bonus**: New users get 10% off their first session!");
            }

            if (ContainsAny(lowerQuestion, new[] { "login", "sign in", "access" }))
            {
                response.AppendLine("**Login Help:**");
                response.AppendLine("- Use your email and password");
                response.AppendLine("- Forgot password? Use 'Reset Password' feature");
                response.AppendLine("- Having issues? Clear browser cache or try different browser");
                response.AppendLine();
                response.AppendLine("🔒 We use secure authentication to protect your account");
            }

            if (ContainsAny(lowerQuestion, new[] { "profile", "update", "edit" }))
            {
                response.AppendLine("**Profile Management:**");
                response.AppendLine("- **Students**: Update learning preferences and goals");
                response.AppendLine("- **Tutors**: Showcase expertise and availability");
                response.AppendLine("- **Both**: Add profile pictures and contact info");
                response.AppendLine();
                response.AppendLine("📊 Complete profiles get better matches and more sessions!");
            }

            return response.ToString();
        }

        private string GeneratePaymentResponse(string question, string lowerQuestion)
        {
            var response = new StringBuilder();

            response.AppendLine("**💳 Payments & Pricing**");
            response.AppendLine();

            response.AppendLine("**Accepted Payment Methods:**");
            response.AppendLine("- 💳 Credit/Debit Cards (Visa, MasterCard, AMEX)");
            response.AppendLine("- 📱 Digital Wallets (PayPal, Apple Pay, Google Pay)");
            response.AppendLine("- 🏦 Bank Transfers (for recurring sessions)");
            response.AppendLine();

            response.AppendLine("**Pricing Structure:**");
            response.AppendLine("- Session rates set by individual tutors");
            response.AppendLine("- Prices shown before booking confirmation");
            response.AppendLine("- No hidden fees or surprise charges");
            response.AppendLine();

            if (ContainsAny(lowerQuestion, new[] { "refund", "cancel" }))
            {
                response.AppendLine("**Refund Policy:**");
                response.AppendLine("- Full refund if canceled 24+ hours before session");
                response.AppendLine("- 50% refund if canceled 6-24 hours before");
                response.AppendLine("- No refund for last-minute cancellations (<6 hours)");
                response.AppendLine();
                response.AppendLine("Contact support for exceptional circumstances");
            }

            response.AppendLine("💎 **Student Discounts**: Ask about our student verification program!");

            return response.ToString();
        }

        private string GenerateTechnicalResponse(string question, string lowerQuestion)
        {
            var response = new StringBuilder();

            response.AppendLine("**🔧 Technical Support**");
            response.AppendLine();

            response.AppendLine("**Quick Troubleshooting:**");
            response.AppendLine("1. **Refresh the page** and try again");
            response.AppendLine("2. **Clear browser cache** and cookies");
            response.AppendLine("3. **Try a different browser** (Chrome, Firefox, Safari)");
            response.AppendLine("4. **Check your internet connection**");
            response.AppendLine();

            response.AppendLine("**Common Issues & Solutions:**");

            if (ContainsAny(lowerQuestion, new[] { "video", "audio", "call" }))
            {
                response.AppendLine("🎥 **Video/Audio Issues:**");
                response.AppendLine("- Allow camera/microphone permissions in browser");
                response.AppendLine("- Test your equipment before sessions");
                response.AppendLine("- Use Chrome or Firefox for best performance");
            }

            if (ContainsAny(lowerQuestion, new[] { "slow", "loading", "lag" }))
            {
                response.AppendLine("⚡ **Performance Issues:**");
                response.AppendLine("- Close other tabs and applications");
                response.AppendLine("- Use wired internet if possible");
                response.AppendLine("- Update your browser to latest version");
            }

            response.AppendLine();
            response.AppendLine("🚨 **Still need help?** Contact our technical team:");
            response.AppendLine("📧 support@tutorconnect.com | 📞 1-800-TUTOR-Help");

            return response.ToString();
        }

        private string GenerateTutorResponse(string question, string lowerQuestion)
        {
            var response = new StringBuilder();

            response.AppendLine("**👨‍🏫 Becoming a Tutor**");
            response.AppendLine();

            response.AppendLine("**Tutor Requirements:**");
            response.AppendLine("- ✅ Subject matter expertise");
            response.AppendLine("- ✅ Teaching experience (preferred)");
            response.AppendLine("- ✅ Reliable internet connection");
            response.AppendLine("- ✅ Professional communication skills");
            response.AppendLine();

            response.AppendLine("**Application Process:**");
            response.AppendLine("1. Submit application with qualifications");
            response.AppendLine("2. Subject knowledge assessment");
            response.AppendLine("3. Interview with our team");
            response.AppendLine("4. Profile setup and training");
            response.AppendLine("5. Start accepting students!");
            response.AppendLine();

            response.AppendLine("**Tutor Benefits:**");
            response.AppendLine("- 💰 Competitive earnings (keep 80% of session fees)");
            response.AppendLine("- 📅 Flexible scheduling (you set your availability)");
            response.AppendLine("- 🌍 Reach students worldwide");
            response.AppendLine("- 🏆 Build your teaching reputation");
            response.AppendLine();

            response.AppendLine("**Ready to apply?** Send your resume and qualifications to:");
            response.AppendLine("📧 tutors@tutorconnect.com");

            return response.ToString();
        }

        private string GenerateFeatureResponse(string question, string lowerQuestion)
        {
            var response = new StringBuilder();

            response.AppendLine("**🌟 TutorConnect Features**");
            response.AppendLine();

            response.AppendLine("**For Students:**");
            response.AppendLine("- 🎯 **Smart Matching**: Find perfect tutors based on your learning style");
            response.AppendLine("- 📊 **Progress Tracking**: Monitor your learning journey");
            response.AppendLine("- ⭐ **Review System**: Choose tutors with proven track records");
            response.AppendLine("- 🔒 **Secure Payments**: Safe and transparent pricing");
            response.AppendLine();

            response.AppendLine("**For Tutors:**");
            response.AppendLine("- 🏠 **Flexible Work**: Teach from anywhere, set your own schedule");
            response.AppendLine("- 📈 **Growth Tools**: Build your reputation and student base");
            response.AppendLine("- 💼 **Resource Library**: Access teaching materials and tools");
            response.AppendLine("- 🤝 **Support Community**: Connect with other educators");
            response.AppendLine();

            response.AppendLine("**Platform Features:**");
            response.AppendLine("- 🎮 **Gamification**: Earn points and achievements for learning");
            response.AppendLine("- 📱 **Mobile Access**: Learn on-the-go with our mobile app");
            response.AppendLine("- 🌐 **Global Community**: Connect with tutors and students worldwide");
            response.AppendLine("- 🔔 **Smart Notifications**: Never miss a session or update");
            response.AppendLine();

            response.AppendLine("💡 **Pro Tip**: Explore our advanced features in the dashboard!");

            return response.ToString();
        }

        private async Task<string> GenerateSmartGenericResponse(string question, string lowerQuestion)
        {
            // Analyze question type and provide contextual help
            if (question.EndsWith("?"))
            {
                if (lowerQuestion.StartsWith("what") || lowerQuestion.StartsWith("which"))
                {
                    return @"**I understand you're asking about TutorConnect features!** 🤔

While I'm constantly learning, here are some areas I can help with:

• **Booking sessions** with qualified tutors
• **Account management** and profile setup  
• **Payment methods** and pricing
• **Technical support** for platform issues
• **Tutor applications** and requirements

Could you be more specific about what you'd like to know? Or try:
- 'How do I book a math session?'
- 'What payment methods do you accept?'
- 'How do I become a tutor?'

For detailed questions, our support team is always ready to help! 📧 support@tutorconnect.com";
                }
                else if (lowerQuestion.StartsWith("how"))
                {
                    return @"**I'd love to help you with that!** 🛠️

For 'how-to' questions about TutorConnect, I can guide you through:

• **Booking your first session**
• **Managing your account settings**  
• **Using platform features**
• **Troubleshooting common issues**
• **Maximizing your learning experience**

Try asking something like:
- 'How do I find available tutors?'
- 'How does the booking process work?'
- 'How can I update my profile?'

The more specific your question, the better I can help! 💪";
                }
                else if (lowerQuestion.StartsWith("when") || lowerQuestion.StartsWith("where"))
                {
                    return @"**Great timing question!** ⏰

For schedule and availability questions:

• Tutors set their own availability (shown in their profiles)
• Sessions can be booked 24/7 based on tutor calendars  
• Most tutors offer evening and weekend slots
• You'll see real-time availability when browsing

Try: 'How do I check a tutor's availability?' for specific guidance!

🌍 **Global Access**: Learn from anywhere in the world!";
                }
            }

            // Default smart response
            return @"**Welcome to TutorConnect Support!** 🤗

I'm here to help you get the most out of our tutoring platform. Based on your question, here are some popular topics I can assist with:

📚 **Learning & Sessions**
• Finding the right tutors for your subjects
• Booking and managing sessions
• Understanding our learning tools

💼 **Account & Profile**  
• Setting up and optimizing your profile
• Account security and preferences
• Notification settings

💰 **Payments & Pricing**
• Understanding our pricing structure
• Payment methods and security
• Refund and cancellation policies

👨‍🏫 **For Tutors**
• Application process and requirements
• Profile optimization tips
• Student matching and scheduling

**Try asking about a specific feature, or contact our human experts for complex questions:** 📧 support@tutorconnect.com

What would you like to know more about?";
        }

        private string GenerateGenericHelpResponse()
        {
            return @"**Hello! I'm TutorBot, your TutorConnect assistant!** 🤖

I can help you with:

• **Booking sessions** with expert tutors
• **Account and profile management**
• **Payment and pricing questions**  
• **Technical support and troubleshooting**
• **Platform features and how-to guides**

**Popular Questions:**
'How do I book my first session?'
'What subjects are available?'
'How does the matching system work?'
'Can I become a tutor?'

**Need immediate human help?**
📧 Email: support@tutorconnect.com  
⏰ Hours: 24/7 support available

What would you like to know today?";
        }

        // Utility method for keyword matching
        private bool ContainsAny(string text, string[] keywords)
        {
            return keywords.Any(keyword => text.Contains(keyword));
        }



        // Helper methods
        private string GetSnippet(string content, string[] keywords)
        {
            var firstMatch = keywords.Select(keyword =>
                content.IndexOf(keyword, StringComparison.OrdinalIgnoreCase))
                .Where(index => index >= 0)
                .OrderBy(index => index)
                .FirstOrDefault();

            if (firstMatch >= 0)
            {
                var start = Math.Max(0, firstMatch - 50);
                var length = Math.Min(150, content.Length - start);
                return "..." + content.Substring(start, length) + "...";
            }

            return content.Length > 150 ? content.Substring(0, 150) + "..." : content;
        }

        private float CalculateRelevance(KnowledgeBaseDocument doc, string[] keywords)
        {
            var score = 0f;
            var contentLower = doc.Content.ToLower();
            var titleLower = doc.Title.ToLower();

            foreach (var keyword in keywords)
            {
                if (titleLower.Contains(keyword)) score += 3f;
                if (contentLower.Contains(keyword)) score += 1f;
                if (doc.Tags.Contains(keyword, StringComparer.OrdinalIgnoreCase)) score += 2f;
            }

            return score;
        }

        private string BuildContextFromDocuments(List<RelevantDocument> docs)
        {
            if (!docs.Any()) return "No specific documentation found for this question.";

            var context = "Relevant information from TutorConnect documentation:\n\n";
            foreach (var doc in docs.Take(3))
            {
                context += $"**{doc.Title}**\n{doc.Snippet}\n\n";
            }
            return context;
        }

        private async Task<string> GetUserContextAsync(int userId, string additionalContext)
        {
            var user = await _context.Users
                .Include(u => u.Student)
                .Include(u => u.Tutor)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            var context = new StringBuilder();

            if (user != null)
            {
                context.AppendLine($"- User Role: {user.Role}");
                if (user.Student != null) context.AppendLine("- Has Student Profile");
                if (user.Tutor != null) context.AppendLine("- Has Tutor Profile");
            }

            if (!string.IsNullOrEmpty(additionalContext))
            {
                context.AppendLine($"- Current Context: {additionalContext}");
            }

            return context.ToString();
        }

        private async Task<List<QuickSuggestion>> GenerateSuggestionsAsync(string question, string userContext)
        {
            var suggestions = new List<QuickSuggestion>();

            // Common suggestions based on question content
            if (question.ToLower().Contains("book") || question.ToLower().Contains("session"))
            {
                suggestions.Add(new QuickSuggestion { Text = "How do I find available tutors?", Type = "question" });
                suggestions.Add(new QuickSuggestion { Text = "What's the cancellation policy?", Type = "question" });
                suggestions.Add(new QuickSuggestion { Text = "Go to Find Tutors", Type = "action", Action = "/tutors" });
            }

            if (question.ToLower().Contains("payment") || question.ToLower().Contains("price"))
            {
                suggestions.Add(new QuickSuggestion { Text = "View pricing plans", Type = "action", Action = "/pricing" });
                suggestions.Add(new QuickSuggestion { Text = "Payment methods accepted", Type = "question" });
            }

            // Add some general suggestions
            suggestions.Add(new QuickSuggestion { Text = "Contact support", Type = "action", Action = "mailto:support@tutorconnect.com" });
            suggestions.Add(new QuickSuggestion { Text = "Browse help articles", Type = "action", Action = "/help" });

            return suggestions.Take(4).ToList();
        }

        private bool ShouldEscalateToHuman(string question, string aiResponse)
        {
            var sensitiveKeywords = new[] { "refund", "complaint", "legal", "urgent", "emergency", "hack", "security" };
            return sensitiveKeywords.Any(keyword => question.ToLower().Contains(keyword)) ||
                   aiResponse.Contains("I don't know") ||
                   aiResponse.Contains("contact support");
        }

        public async Task<ConversationDTO> GetConversationAsync(int conversationId, int userId)
        {
            var conversation = await _context.ChatbotConversations
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(c => c.ConversationId == conversationId && c.UserId == userId);

            if (conversation == null)
                return null;

            return new ConversationDTO
            {
                ConversationId = conversation.ConversationId,
                Title = conversation.Title,
                UpdatedAt = conversation.UpdatedAt,
                Messages = conversation.Messages
                    .OrderBy(m => m.SentAt)
                    .Select(m => new ChatbotMessageDTO
                    {
                        Content = m.Content,
                        IsUserMessage = m.IsUserMessage,
                        SentAt = m.SentAt,
                        MessageType = m.MessageType
                    })
                    .ToList()
            };
        }

        public async Task<bool> DeleteConversationAsync(int conversationId, int userId)
        {
            var conversation = await _context.ChatbotConversations
                .FirstOrDefaultAsync(c => c.ConversationId == conversationId && c.UserId == userId);

            if (conversation == null)
                return false;

            _context.ChatbotConversations.Remove(conversation);
            await _context.SaveChangesAsync();
            return true;
        }

        // Add these helper methods to ChatbotService:
        private async Task<string> GenerateConversationTitleAsync(int conversationId)
        {
            var firstMessage = await _context.ChatbotMessages
                .Where(m => m.ConversationId == conversationId && m.IsUserMessage)
                .OrderBy(m => m.SentAt)
                .FirstOrDefaultAsync();

            if (firstMessage != null && firstMessage.Content.Length > 30)
            {
                return firstMessage.Content.Substring(0, 30) + "...";
            }

            return firstMessage?.Content ?? "New Conversation";
        }

        private async Task<string> GenerateInitialTitleAsync(string firstQuestion)
        {
            if (firstQuestion.Length > 30)
            {
                return firstQuestion.Substring(0, 30) + "...";
            }
            return firstQuestion;
        }

        public async Task<List<ConversationDTO>> GetUserConversationsAsync(int userId)
        {
            return await _context.ChatbotConversations
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.UpdatedAt)
                .Select(c => new ConversationDTO
                {
                    ConversationId = c.ConversationId,
                    Title = c.Title,
                    UpdatedAt = c.UpdatedAt,
                    Messages = c.Messages
                        .OrderBy(m => m.SentAt)
                        .Select(m => new ChatbotMessageDTO
                        {
                            Content = m.Content,
                            IsUserMessage = m.IsUserMessage,
                            SentAt = m.SentAt,
                            MessageType = m.MessageType
                        })
                        .ToList()
                })
                .ToListAsync();
        }

        public async Task TrainKnowledgeBaseAsync()
        {
            // This would be called to initially populate the knowledge base
            // You can add your platform documentation here
            var defaultDocs = new List<KnowledgeBaseDocument>
        {
            new KnowledgeBaseDocument
            {
                Title = "How to Book a Tutoring Session",
                Content = @"Booking a session on TutorConnect is easy! Follow these steps:
1. Go to the Find Tutors page
2. Search by subject, module, or tutor name
3. View tutor profiles, ratings, and availability
4. Select your preferred date and time slot
5. Confirm your booking
You'll receive email confirmation and can view the session in your dashboard.",
                DocumentType = "tutorial",
                Category = "Booking",
                Tags = new List<string> { "booking", "session", "schedule", "how-to" }
            },
            // Add more documentation as needed...
        };

            _context.KnowledgeBaseDocuments.AddRange(defaultDocs);
            await _context.SaveChangesAsync();
        }
    }

    // OpenAI Response Model
    public class OpenAIResponse
    {
        public Choice[] choices { get; set; }
    }

    public class Choice
    {
        public Message message { get; set; }
    }

    public class Message
    {
        public string content { get; set; }
    }



}
