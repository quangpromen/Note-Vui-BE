using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NoteVui.Application.DTOs.Ai;
using NoteVui.Application.Interfaces;

namespace NoteVui.Infrastructure.Services;

/// <summary>
/// Implementation of IAiService using Google Gemini API.
/// This service handles all AI-related operations through the Gemini API.
/// </summary>
public class GeminiAiService : IAiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GeminiAiService> _logger;
    private readonly string _apiKey;
    private readonly string _apiUrl;

    // Placeholder values that indicate the API key is not configured
    private static readonly string[] InvalidKeyPlaceholders = 
    {
        "YOUR_GEMINI_API_KEY_HERE",
        "YOUR_KEY_HERE",
        "",
        "REPLACE_ME"
    };

    public GeminiAiService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GeminiAiService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Retrieve API Key safely from configuration
        _apiKey = configuration["AiSettings:GeminiApiKey"] ?? string.Empty;
        _apiUrl = configuration["AiSettings:GeminiApiUrl"] 
            ?? "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent";

        // Validate the API key - DO NOT expose the key in error messages
        ValidateApiKey();
    }

    /// <summary>
    /// Validates that the API key is properly configured.
    /// Throws a generic exception to avoid exposing configuration details.
    /// </summary>
    private void ValidateApiKey()
    {
        if (string.IsNullOrWhiteSpace(_apiKey) || 
            InvalidKeyPlaceholders.Any(p => _apiKey.Equals(p, StringComparison.OrdinalIgnoreCase)))
        {
            _logger.LogError("Gemini API key is not configured properly. Please set the key in appsettings.Development.json or User Secrets.");
            throw new InvalidOperationException("Server AI configuration is missing or invalid. Please contact the administrator.");
        }
    }

    public async Task<AiResponse> SummarizeAsync(string content, CancellationToken cancellationToken = default)
    {
        var prompt = $"Please provide a concise summary of the following text. Keep the summary clear and focused on the main points:\n\n{content}";
        return await CallGeminiApiAsync(prompt, cancellationToken);
    }

    public async Task<AiResponse> FixGrammarAsync(string content, CancellationToken cancellationToken = default)
    {
        var prompt = $"Please correct any grammar, spelling, and punctuation errors in the following text. Return only the corrected text without any explanations:\n\n{content}";
        return await CallGeminiApiAsync(prompt, cancellationToken);
    }

    public async Task<AiResponse> TranslateAsync(string content, string targetLanguage, CancellationToken cancellationToken = default)
    {
        var languageName = GetLanguageName(targetLanguage);
        var prompt = $"Please translate the following text to {languageName}. Return only the translated text without any explanations:\n\n{content}";
        return await CallGeminiApiAsync(prompt, cancellationToken);
    }

    public async Task<AiResponse> GenerateIdeasAsync(string content, CancellationToken cancellationToken = default)
    {
        var prompt = $"Based on the following text, please generate creative ideas, suggestions, or related topics that could be explored further. Format them as a numbered list:\n\n{content}";
        return await CallGeminiApiAsync(prompt, cancellationToken);
    }

    /// <summary>
    /// Makes the actual API call to Google Gemini.
    /// </summary>
    private async Task<AiResponse> CallGeminiApiAsync(string prompt, CancellationToken cancellationToken)
    {
        try
        {
            var requestUrl = $"{_apiUrl}?key={_apiKey}";

            var requestBody = new GeminiRequest
            {
                Contents = new List<GeminiContent>
                {
                    new GeminiContent
                    {
                        Parts = new List<GeminiPart>
                        {
                            new GeminiPart { Text = prompt }
                        }
                    }
                },
                GenerationConfig = new GeminiGenerationConfig
                {
                    Temperature = 0.7,
                    MaxOutputTokens = 2048,
                    TopP = 0.95,
                    TopK = 40
                }
            };

            var response = await _httpClient.PostAsJsonAsync(requestUrl, requestBody, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Gemini API error: {StatusCode} - {Error}", response.StatusCode, errorContent);
                
                // Return generic error to avoid exposing API details
                return AiResponse.Failure("AI service is temporarily unavailable. Please try again later.");
            }

            var geminiResponse = await response.Content.ReadFromJsonAsync<GeminiResponse>(cancellationToken: cancellationToken);

            if (geminiResponse?.Candidates == null || !geminiResponse.Candidates.Any())
            {
                return AiResponse.Failure("AI service returned an empty response. Please try again.");
            }

            var resultText = geminiResponse.Candidates[0].Content?.Parts?.FirstOrDefault()?.Text ?? string.Empty;
            
            // Extract token usage if available
            int inputTokens = geminiResponse.UsageMetadata?.PromptTokenCount ?? 0;
            int outputTokens = geminiResponse.UsageMetadata?.CandidatesTokenCount ?? 0;

            return AiResponse.Success(resultText, inputTokens, outputTokens);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error while calling Gemini API");
            return AiResponse.Failure("Unable to connect to AI service. Please check your network connection.");
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "Timeout while calling Gemini API");
            return AiResponse.Failure("AI service request timed out. Please try again.");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error parsing Gemini API response");
            return AiResponse.Failure("AI service returned an invalid response. Please try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while calling Gemini API");
            return AiResponse.Failure("An unexpected error occurred. Please try again later.");
        }
    }

    /// <summary>
    /// Gets the full language name from a language code.
    /// </summary>
    private static string GetLanguageName(string languageCode)
    {
        return languageCode?.ToLowerInvariant() switch
        {
            "en" => "English",
            "vi" => "Vietnamese",
            "ja" => "Japanese",
            "ko" => "Korean",
            "zh" => "Chinese",
            "fr" => "French",
            "de" => "German",
            "es" => "Spanish",
            "it" => "Italian",
            "pt" => "Portuguese",
            "ru" => "Russian",
            "ar" => "Arabic",
            "th" => "Thai",
            _ => languageCode ?? "English"
        };
    }

    #region Gemini API Models

    private class GeminiRequest
    {
        [JsonPropertyName("contents")]
        public List<GeminiContent> Contents { get; set; } = new();

        [JsonPropertyName("generationConfig")]
        public GeminiGenerationConfig? GenerationConfig { get; set; }
    }

    private class GeminiContent
    {
        [JsonPropertyName("parts")]
        public List<GeminiPart> Parts { get; set; } = new();
    }

    private class GeminiPart
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }

    private class GeminiGenerationConfig
    {
        [JsonPropertyName("temperature")]
        public double Temperature { get; set; } = 0.7;

        [JsonPropertyName("maxOutputTokens")]
        public int MaxOutputTokens { get; set; } = 2048;

        [JsonPropertyName("topP")]
        public double TopP { get; set; } = 0.95;

        [JsonPropertyName("topK")]
        public int TopK { get; set; } = 40;
    }

    private class GeminiResponse
    {
        [JsonPropertyName("candidates")]
        public List<GeminiCandidate>? Candidates { get; set; }

        [JsonPropertyName("usageMetadata")]
        public GeminiUsageMetadata? UsageMetadata { get; set; }
    }

    private class GeminiCandidate
    {
        [JsonPropertyName("content")]
        public GeminiContent? Content { get; set; }
    }

    private class GeminiUsageMetadata
    {
        [JsonPropertyName("promptTokenCount")]
        public int PromptTokenCount { get; set; }

        [JsonPropertyName("candidatesTokenCount")]
        public int CandidatesTokenCount { get; set; }

        [JsonPropertyName("totalTokenCount")]
        public int TotalTokenCount { get; set; }
    }

    #endregion
}
