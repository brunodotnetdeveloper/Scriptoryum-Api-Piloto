namespace Scriptoryum.Api.Domain.Enums;

/// <summary>
/// Status de um usuário dentro de uma organização
/// </summary>
public enum OrganizationUserStatus
{
    /// <summary>
    /// Usuário ativo na organização
    /// </summary>
    Active = 0,

    /// <summary>
    /// Usuário inativo na organização
    /// </summary>
    Inactive = 1,

    /// <summary>
    /// Usuário suspenso na organização
    /// </summary>
    Suspended = 2,

    /// <summary>
    /// Usuário removido da organização
    /// </summary>
    Removed = 3,

    /// <summary>
    /// Convite pendente para o usuário
    /// </summary>
    PendingInvitation = 4
}