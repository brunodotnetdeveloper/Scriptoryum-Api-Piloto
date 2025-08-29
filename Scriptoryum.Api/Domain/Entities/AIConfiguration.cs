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
    
    // Configuração pode ser associada a uma organização ou workspace
    public int? OrganizationId { get; set; }
    public Organization? Organization { get; set; }
    
    public int? WorkspaceId { get; set; }
    public Workspace? Workspace { get; set; }
}