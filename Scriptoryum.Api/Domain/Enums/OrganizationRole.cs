namespace Scriptoryum.Api.Domain.Enums;

/// <summary>
/// Papel de um usuário dentro de uma organização
/// </summary>
public enum OrganizationRole
{
    /// <summary>
    /// Usuário comum da organização
    /// </summary>
    Member = 0,

    /// <summary>
    /// Administrador da organização
    /// </summary>
    Admin = 1,

    /// <summary>
    /// Proprietário/Dono da organização
    /// </summary>
    Owner = 2
}