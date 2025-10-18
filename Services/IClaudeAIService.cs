using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TutorConnectAPI.DTOs;

namespace TutorConnectAPI.Services
{
    public interface IClaudeAIService
    {
        Task<string> GenerateChatResponseAsync(string prompt);
        Task<ChatResponse> ProcessChatMessageAsync(ChatQuestionRequest request);
    }

    public class ClaudeAIService : IClaudeAIService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ClaudeAIService> _logger;
        private const string CLAUDE_API_URL = "https://api.anthropic.com/v1/messages";

        public ClaudeAIService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<ClaudeAIService> logger)
        {
            _httpClient = httpClientFactory.CreateClient();
            _configuration = configuration;
            _logger = logger;

            // ✅ FIXED: Don't set BaseAddress, use full URL instead
            // Set headers only
            var apiKey = _configuration["Claude:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogError("Claude API Key is not configured!");
            }
            else
            {
                _logger.LogInformation("Claude API Key configured (length: {Length})", apiKey.Length);
            }

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
            _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
            _httpClient.Timeout = TimeSpan.FromSeconds(60);
        }

        public async Task<string> GenerateChatResponseAsync(string prompt)
        {
            try
            {
                _logger.LogInformation("=== CLAUDE API CALL STARTED ===");
                _logger.LogInformation("Prompt length: {Length} chars", prompt.Length);
                _logger.LogInformation("Target URL: {Url}", CLAUDE_API_URL);

                var model = _configuration["Claude:Model"] ?? "claude-3-5-sonnet-20241022";
                var maxTokens = int.Parse(_configuration["Claude:MaxTokens"] ?? "2000");
                var temperature = double.Parse(_configuration["Claude:Temperature"] ?? "0.7");

                _logger.LogInformation("Model: {Model}, MaxTokens: {MaxTokens}, Temp: {Temp}",
                    model, maxTokens, temperature);

                var requestBody = new
                {
                    model = model,
                    max_tokens = maxTokens,
                    temperature = temperature,
                    messages = new[]
                    {
                        new
                        {
                            role = "user",
                            content = prompt
                        }
                    }
                };

                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    WriteIndented = false
                };

                var jsonContent = JsonSerializer.Serialize(requestBody, jsonOptions);
                _logger.LogInformation("Request body: {Json}", jsonContent);

                // Log headers (without exposing full API key)
                var apiKey = _configuration["Claude:ApiKey"];
                _logger.LogInformation("API Key present: {Present}, Length: {Length}",
                    !string.IsNullOrEmpty(apiKey), apiKey?.Length ?? 0);

                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _logger.LogInformation("Sending POST request to: {Url}", CLAUDE_API_URL);
                var response = await _httpClient.PostAsync(CLAUDE_API_URL, content);

                _logger.LogInformation("Response Status: {StatusCode} ({StatusCodeInt})",
                    response.StatusCode, (int)response.StatusCode);

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Response content length: {Length}", responseContent?.Length ?? 0);
                _logger.LogInformation("Response content: {Content}", responseContent);

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var deserializeOptions = new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                            PropertyNameCaseInsensitive = true
                        };

                        var result = JsonSerializer.Deserialize<ClaudeResponse>(responseContent, deserializeOptions);

                        if (result?.Content != null && result.Content.Length > 0)
                        {
                            var text = result.Content[0].Text;
                            _logger.LogInformation("✅ Successfully extracted response: {Length} chars", text?.Length ?? 0);
                            return text ?? "Empty response from Claude";
                        }
                        else
                        {
                            _logger.LogWarning("❌ Claude returned empty content array");
                            return "I received an unexpected response format from the AI service.";
                        }
                    }
                    catch (JsonException jsonEx)
                    {
                        _logger.LogError(jsonEx, "❌ Failed to parse Claude response JSON");
                        return "I received an invalid response format from the AI service.";
                    }
                }
                else
                {
                    _logger.LogError("❌ Claude API returned error: {StatusCode}", response.StatusCode);
                    _logger.LogError("Error content: {ErrorContent}", responseContent);

                    // Try to parse error details
                    try
                    {
                        var errorObj = JsonSerializer.Deserialize<JsonElement>(responseContent);
                        if (errorObj.TryGetProperty("error", out var errorProp))
                        {
                            var errorMsg = errorProp.GetProperty("message").GetString();
                            _logger.LogError("Claude error message: {Message}", errorMsg);
                        }
                    }
                    catch { }

                    return response.StatusCode switch
                    {
                        System.Net.HttpStatusCode.NotFound =>
                            "The AI service endpoint was not found. Please check the API configuration.",
                        System.Net.HttpStatusCode.Unauthorized =>
                            "AI service authentication failed. Please check the API key.",
                        System.Net.HttpStatusCode.BadRequest =>
                            $"Invalid request to AI service. Details: {responseContent}",
                        System.Net.HttpStatusCode.TooManyRequests =>
                            "Rate limit exceeded. Please try again in a moment.",
                        _ =>
                            $"AI service error ({response.StatusCode}): {responseContent}"
                    };
                }
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "❌ HTTP request failed");
                return "I cannot reach the AI service. Please check your internet connection.";
            }
            catch (TaskCanceledException timeoutEx)
            {
                _logger.LogError(timeoutEx, "❌ Request timed out");
                return "The AI service request timed out. Please try again.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Unexpected error calling Claude API");
                return $"Technical error: {ex.Message}";
            }
        }

        public async Task<ChatResponse> ProcessChatMessageAsync(ChatQuestionRequest request)
        {
            var response = await GenerateChatResponseAsync(request.Question);

            return new ChatResponse
            {
                Answer = response ?? "I apologize, but I'm having trouble generating a response right now.",
                RequiresHumanSupport = string.IsNullOrEmpty(response) ||
                                      response.Contains("Error:") ||
                                      response.Contains("trouble") ||
                                      response.Contains("error")
            };
        }
    }

    // Response models with explicit JSON property names
    public class ClaudeResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("content")]
        public ClaudeContent[] Content { get; set; }

        [JsonPropertyName("model")]
        public string Model { get; set; }

        [JsonPropertyName("stop_reason")]
        public string StopReason { get; set; }

        [JsonPropertyName("stop_sequence")]
        public string StopSequence { get; set; }

        [JsonPropertyName("usage")]
        public ClaudeUsage Usage { get; set; }
    }

    public class ClaudeContent
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; }
    }

    public class ClaudeUsage
    {
        [JsonPropertyName("input_tokens")]
        public int InputTokens { get; set; }

        [JsonPropertyName("output_tokens")]
        public int OutputTokens { get; set; }
    }
}