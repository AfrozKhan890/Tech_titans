using System.Text;
using System.Text.Json;
using MarketLink.Models;
using Microsoft.Extensions.Options;

namespace MarketLink.Services.Ai;






public class GeminiAiAssistantService : IAiAssistantService
{
    private readonly HttpClient _http;
    private readonly AiSettings _settings;
    private readonly ILogger<GeminiAiAssistantService> _logger;

    public GeminiAiAssistantService(HttpClient http, IOptions<AiSettings> options, ILogger<GeminiAiAssistantService> logger)
    {
        _http = http;
        _settings = options.Value;
        _logger = logger;
    }

    public async Task<AiReplyResult> AskAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            _logger.LogWarning("AI assistant called but Ai:ApiKey is not configured.");
            return new AiReplyResult(false, "The assistant isn't configured yet. Please contact support.");
        }

        var model = string.IsNullOrWhiteSpace(_settings.Model) ? "gemini-flash-lite-latest" : _settings.Model;
        var maxTokens = _settings.MaxTokens > 0 ? _settings.MaxTokens : 1024;


        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{Uri.EscapeDataString(model)}:generateContent?key={_settings.ApiKey}";

        var payload = new
        {
            systemInstruction = new { parts = new[] { new { text = systemPrompt } } },
            contents = new[]
            {
                new { role = "user", parts = new[] { new { text = userMessage } } }
            },
            generationConfig = new { maxOutputTokens = maxTokens, temperature = 0.4 }
        };

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(20));

            using var response = await _http.SendAsync(request, timeoutCts.Token);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Gemini returned {StatusCode}: {Body}", response.StatusCode, Truncate(body));
                return new AiReplyResult(false, "The assistant is temporarily unavailable. Please try again in a moment.");
            }

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            if (!root.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
            {
                var blockReason = root.TryGetProperty("promptFeedback", out var feedback) &&
                                   feedback.TryGetProperty("blockReason", out var reason)
                    ? reason.GetString()
                    : null;
                _logger.LogInformation("Gemini returned no candidates. blockReason={BlockReason}", blockReason);
                return new AiReplyResult(false, "I couldn't come up with an answer to that - could you rephrase your question?");
            }

            var firstCandidate = candidates[0];
            string? text = null;
            if (firstCandidate.TryGetProperty("content", out var content) &&
                content.TryGetProperty("parts", out var parts) &&
                parts.GetArrayLength() > 0 &&
                parts[0].TryGetProperty("text", out var textEl))
            {
                text = textEl.GetString();
            }

            return new AiReplyResult(true, string.IsNullOrWhiteSpace(text) ? "I'm not sure how to answer that." : text.Trim());
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("AI assistant request timed out.");
            return new AiReplyResult(false, "The assistant took too long to respond. Please try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI assistant request failed.");
            return new AiReplyResult(false, "Something went wrong reaching the assistant. Please try again.");
        }
    }

    private static string Truncate(string s) => s.Length > 300 ? s[..300] : s;
}
