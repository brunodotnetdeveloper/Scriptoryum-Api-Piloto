using Scriptoryum.Api.Domain.Enums;

namespace Scriptoryum.Api.Domain.Entities;

/// <summary>
/// Representa uma organização no sistema B2B
/// </summary>
public class Organization : EntityBase
{
    /// <summary>
    /// Nome da organização
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// CNPJ da organização (opcional)
    /// </summary>
    public string Cnpj { get; set; }

    /// <summary>
    /// Email de contato da organização
    /// </summary>
    public string ContactEmail { get; set; } = string.Empty;

    /// <summary>
    /// Telefone de contato da organização (opcional)
    /// </summary>
    public string ContactPhone { get; set; }

    /// <summary>
    /// Endereço da organização (opcional)
    /// </summary>
    public string Address { get; set; }

    /// <summary>
    /// Status da organização
    /// </summary>
    public OrganizationStatus Status { get; set; } = OrganizationStatus.Active;

    /// <summary>
    /// Usuários que pertencem a esta organização
    /// </summary>
    public ICollection<ApplicationUser> Users { get; set; } = [];

    /// <summary>
    /// Workspaces que pertencem a esta organização
    /// </summary>
    public ICollection<Workspace> Workspaces { get; set; } = [];

    /// <summary>
    /// Configurações de provedores de IA da organização
    /// </summary>
    public ICollection<OrganizationAIProviderConfig> AIProviderConfigs { get; set; } = [];
    
    /// <summary>
    /// Configurações de IA compartilhadas da organização
    /// </summary>
    public ICollection<AIConfiguration> AIConfigurations { get; set; } = [];
}