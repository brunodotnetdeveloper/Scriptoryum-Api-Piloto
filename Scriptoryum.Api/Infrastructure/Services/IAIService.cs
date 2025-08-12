using Scriptoryum.Api.Domain.Enums;

namespace Scriptoryum.Api.Infrastructure.Services;

public interface IAIService
{
    Task<AIResponse> GenerateResponseAsync(AIRequest request);
    Task<float[]> GenerateEmbeddingAsync(string text, string apiKey, AIProvider provider);
}

public class AIRequest
{
    public string Message { get; set; } = string.Empty;
    public string? Context { get; set; }
    public string Model { get; set; } = string.Empty;
    public float Temperature { get; set; } = 0.7f;
    public int MaxTokens { get; set; } = 4000;
    public string ApiKey { get; set; } = string.Empty;
    public AIProvider Provider { get; set; }
}

public class AIResponse
{
    public string Response { get; set; } = string.Empty;
    public int TokensUsed { get; set; }
    public decimal Cost { get; set; }
    public string Model { get; set; } = string.Empty;
    public int ResponseTimeMs { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public AIProvider Provider { get; set; }
}