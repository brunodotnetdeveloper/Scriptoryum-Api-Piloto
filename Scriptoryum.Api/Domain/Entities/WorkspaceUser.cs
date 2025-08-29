using Scriptoryum.Api.Domain.Enums;

namespace Scriptoryum.Api.Domain.Entities;

/// <summary>
/// Representa a relação entre um workspace e um usuário
/// </summary>
public class WorkspaceUser : EntityBase
{
    /// <summary>
    /// ID do workspace
    /// </summary>
    public int WorkspaceId { get; set; }

    /// <summary>
    /// Referência para o workspace
    /// </summary>
    public Workspace Workspace { get; set; }

    /// <summary>
    /// ID do usuário
    /// </summary>
    public string UserId { get; set; }

    /// <summary>
    /// Referência para o usuário
    /// </summary>
    public ApplicationUser User { get; set; }

    /// <summary>
    /// Papel do usuário no workspace
    /// </summary>
    public string Role { get; set; } = WorkspaceRole.Member.ToString();

    /// <summary>
    /// Status do usuário no workspace
    /// </summary>
    public string Status { get; set; } = WorkspaceUserStatus.Active.ToString();

    /// <summary>
    /// Data em que o usuário foi adicionado ao workspace
    /// </summary>
    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Data em que o usuário foi removido do workspace (se aplicável)
    /// </summary>
    public DateTimeOffset? RemovedAt { get; set; }
}