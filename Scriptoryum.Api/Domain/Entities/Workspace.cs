using Scriptoryum.Api.Domain.Enums;

namespace Scriptoryum.Api.Domain.Entities;

/// <summary>
/// Representa um workspace dentro de uma organização
/// </summary>
public class Workspace : EntityBase
{
    /// <summary>
    /// Nome do workspace
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Descrição do workspace
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Status do workspace
    /// </summary>
    public WorkspaceStatus Status { get; set; } = WorkspaceStatus.Active;

    /// <summary>
    /// ID da organização à qual este workspace pertence
    /// </summary>
    public int OrganizationId { get; set; }

    /// <summary>
    /// Referência para a organização
    /// </summary>
    public Organization Organization { get; set; }

    /// <summary>
    /// Usuários que têm acesso a este workspace
    /// </summary>
    public ICollection<WorkspaceUser> WorkspaceUsers { get; set; } = [];

    /// <summary>
    /// Documentos que pertencem a este workspace
    /// </summary>
    public ICollection<Document> Documents { get; set; } = [];

    /// <summary>
    /// Sessões de chat que acontecem neste workspace
    /// </summary>
    public ICollection<ChatSession> ChatSessions { get; set; } = [];

    /// <summary>
    /// Chaves de API específicas deste workspace
    /// </summary>
    public ICollection<ServiceApiKey> ServiceApiKeys { get; set; } = [];

    /// <summary>
    /// Configurações de IA específicas deste workspace
    /// </summary>
    public ICollection<AIConfiguration> AIConfigurations { get; set; } = [];
}