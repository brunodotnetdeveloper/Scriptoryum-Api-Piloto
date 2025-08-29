namespace Scriptoryum.Api.Domain.Enums;

/// <summary>
/// Papel de um usuário dentro de um workspace
/// </summary>
public enum WorkspaceRole
{
    /// <summary>
    /// Visualizador do workspace (apenas leitura)
    /// </summary>
    Viewer = 0,

    /// <summary>
    /// Membro do workspace (pode criar e editar conteúdo)
    /// </summary>
    Member = 1,

    /// <summary>
    /// Administrador do workspace (pode gerenciar usuários e configurações)
    /// </summary>
    Admin = 2
}