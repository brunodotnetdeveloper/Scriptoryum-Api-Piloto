namespace Scriptoryum.Api.Domain.Enums;

/// <summary>
/// Status de uma chave de API de serviço
/// </summary>
public enum ServiceApiKeyStatus
{
    /// <summary>
    /// Chave ativa e funcional
    /// </summary>
    Active = 0,

    /// <summary>
    /// Chave desativada temporariamente
    /// </summary>
    Inactive = 1,

    /// <summary>
    /// Chave revogada permanentemente
    /// </summary>
    Revoked = 2,

    /// <summary>
    /// Chave expirada
    /// </summary>
    Expired = 3,

    /// <summary>
    /// Chave suspensa por uso excessivo
    /// </summary>
    Suspended = 4
}