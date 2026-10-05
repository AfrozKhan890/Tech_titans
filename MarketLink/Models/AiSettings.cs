namespace MarketLink.Models;






public class AiSettings
{
    public string Provider { get; set; } = "Gemini";
    public string Model { get; set; } = "gemini-flash-lite-latest";
    public int MaxTokens { get; set; } = 1024;
    public string ApiKey { get; set; } = string.Empty;
}
