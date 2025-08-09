using Scriptoryum.Api.Domain.Enums;

namespace Scriptoryum.Api.Domain.Entities;

public class AIConfiguration : EntityBase
{
    public string UserId { get; set; }
    public string MaxTokens { get; set; }
    public string Temperature { get; set; }

    public ApplicationUser User { get; set; }
    public string DefaultProvider { get; set; }
    public ICollection<AIProviderConfig> AIProviderConfigs { get; set; } = [];
}