namespace Scriptoryum.Api.Domain.Enums;

/// <summary>
/// Status de uma organização no sistema
/// </summary>
public enum OrganizationStatus
{
    /// <summary>
    /// Organização ativa e operacional
    /// </summary>
    Active = 0,

    /// <summary>
    /// Organização inativa temporariamente
    /// </summary>
    Inactive = 1,

    /// <summary>
    /// Organização suspensa
    /// </summary>
    Suspended = 2,

    /// <summary>
    /// Organização cancelada/removida
    /// </summary>
    Cancelled = 3,

    Deleted = 4
}