using Scriptoryum.Api.Domain.Enums;

namespace Scriptoryum.Api.Infrastructure.Services;

public interface IOpenAIService
{
    Task<OpenAIResponse> GenerateResponseAsync(OpenAIRequest request);
    Task<float[]> GenerateEmbeddingAsync(string text, string apiKey);
}

public class OpenAIRequest
{
    public string Message { get; set; } = string.Empty;
    public string? Context { get; set; }
    public string Model { get; set; } = "gpt-4o-mini";
    public float Temperature { get; set; } = 0.7f;
    public int MaxTokens { get; set; } = 4000;
    public string ApiKey { get; set; } = string.Empty;
}

public class OpenAIResponse
{
    public string Response { get; set; } = string.Empty;
    public int TokensUsed { get; set; }
    public decimal Cost { get; set; }
    public string Model { get; set; } = string.Empty;
    public int ResponseTimeMs { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}