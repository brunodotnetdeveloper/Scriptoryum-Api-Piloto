using Scriptoryum.Api.Domain.Enums;

namespace Scriptoryum.Api.Domain.Entities;

public class AIConfiguration : EntityBase
{
    public string UserId { get; set; }
    public string OpenAIApiKey { get; set; }
    public string OpenAIModel { get; set; }
    public string ClaudeApiKey { get; set; }
    public string ClaudeModel { get; set; }
    public string GeminiApiKey { get; set; }
    public string GeminiModel { get; set; }
    public string MaxTokens { get; set; }
    public string Temperature { get; set; }

    public ApplicationUser User { get; set; }
    public AIProvider DefaultProvider { get; set; }
    public ICollection<AIProviderConfig> AIProviderConfigs { get; set; } = [];
}