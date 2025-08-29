namespace Scriptoryum.Api.Domain.Enums;

/// <summary>
/// Status de um workspace
/// </summary>
public enum WorkspaceStatus
{
    /// <summary>
    /// Workspace ativo e operacional
    /// </summary>
    Active = 0,

    /// <summary>
    /// Workspace arquivado (somente leitura)
    /// </summary>
    Archived = 1,

    /// <summary>
    /// Workspace suspenso temporariamente
    /// </summary>
    Suspended = 2,

    /// <summary>
    /// Workspace removido
    /// </summary>
    Deleted = 3
}