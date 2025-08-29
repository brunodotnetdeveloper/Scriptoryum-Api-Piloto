namespace Scriptoryum.Api.Domain.Enums;

/// <summary>
/// Status de um usuário dentro de um workspace
/// </summary>
public enum WorkspaceUserStatus
{
    /// <summary>
    /// Usuário ativo no workspace
    /// </summary>
    Active = 0,

    /// <summary>
    /// Usuário inativo no workspace
    /// </summary>
    Inactive = 1,

    /// <summary>
    /// Usuário suspenso no workspace
    /// </summary>
    Suspended = 2,

    /// <summary>
    /// Usuário removido do workspace
    /// </summary>
    Removed = 3,

    /// <summary>
    /// Convite pendente para o workspace
    /// </summary>
    PendingInvitation = 4
}