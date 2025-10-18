using System.Text;
using System.Text.Json;

namespace TutorConnectAPI.Services
{
    public interface IGeminiAIService
    {
        Task<string> GenerateChatResponseAsync(string prompt);
        Task<List<string>> GetAvailableModelsAsync();
    }

    public class GeminiAIService : IGeminiAIService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _model;
        private readonly ILogger<GeminiAIService> _logger;

        public GeminiAIService(IConfiguration configuration, ILogger<GeminiAIService> logger)
        {
            _httpClient = new HttpClient();
            _apiKey = configuration["Gemini:ApiKey"];
            _model = configuration["Gemini:Model"] ?? "gemini-pro";
            _logger = logger;
        }

        public async Task<string> GenerateChatResponseAsync(string prompt)
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                _logger.LogWarning("Gemini API key not configured");
                return null;
            }

            // List of possible model names to try
            var modelNames = new[]
            {
             "gemini-1.5-pro",
            "gemini-1.0-pro",
               "gemini-pro",
            "models/gemini-pro" };
    

            foreach (var modelName in modelNames)
            {
                try
                {
                    Console.WriteLine($"Trying model: {modelName}");

                    var requestBody = new
                    {
                        contents = new[]
                        {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                },
                        generationConfig = new
                        {
                            temperature = 0.7,
                            topK = 40,
                            topP = 0.95,
                            maxOutputTokens = 500
                        }
                    };

                    // Try different URL formats
                    var url = $"https://generativelanguage.googleapis.com/v1/models/{modelName}:generateContent?key={_apiKey}";

                    Console.WriteLine($"Calling URL: {url.Replace(_apiKey, "API_KEY_REDACTED")}");

                    var response = await _httpClient.PostAsJsonAsync(url, requestBody);
                    var responseContent = await response.Content.ReadAsStringAsync();

                    Console.WriteLine($"Response Status: {response.StatusCode}");

                    if (response.IsSuccessStatusCode)
                    {
                        var result = JsonSerializer.Deserialize<GeminiResponse>(responseContent, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        var responseText = result?.Candidates?[0]?.Content?.Parts?[0]?.Text;

                        if (!string.IsNullOrEmpty(responseText))
                        {
                            Console.WriteLine($"✅ SUCCESS with model: {modelName}");
                            Console.WriteLine($"Response: {responseText}");
                            return responseText.Trim();
                        }
                    }
                    else
                    {
                        Console.WriteLine($"❌ Failed with {modelName}: {responseContent}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error with {modelName}: {ex.Message}");
                }
            }

            Console.WriteLine("❌ All model attempts failed");
            return null;
        }


        public async Task<List<string>> GetAvailableModelsAsync()
        {
            try
            {
                var url = $"https://generativelanguage.googleapis.com/v1/models?key={_apiKey}";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Available models: {content}");

                    // Parse the response to see available models
                    var modelsData = JsonSerializer.Deserialize<ModelsListResponse>(content);
                    return modelsData?.Models?.Select(m => m.Name).ToList() ?? new List<string>();
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error getting models: {error}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception getting models: {ex.Message}");
            }

            return new List<string>();
        }

        public class ModelsListResponse
        {
            public Model[] Models { get; set; }
        }

        public class Model
        {
            public string Name { get; set; }
            public string Version { get; set; }
            public string DisplayName { get; set; }
            public string Description { get; set; }
            public string InputTokenLimit { get; set; }
            public string OutputTokenLimit { get; set; }
            public string[] SupportedGenerationMethods { get; set; }
        }



        // Response classes
        public class GeminiResponse
        {
            public Candidate[] Candidates { get; set; }
        }

        public class Candidate
        {
            public Content Content { get; set; }
        }

        public class Content
        {
            public Part[] Parts { get; set; }
        }

        public class Part
        {
            public string Text { get; set; }
        }
    }
}