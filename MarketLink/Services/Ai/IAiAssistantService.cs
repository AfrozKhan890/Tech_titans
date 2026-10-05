namespace MarketLink.Services.Ai;






public record AiReplyResult(bool Success, string Message);






public interface IAiAssistantService
{
    Task<AiReplyResult> AskAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken = default);
}
