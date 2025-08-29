using Scriptoryum.Api.Domain.Enums;

namespace Scriptoryum.Api.Domain.Entities;

/// <summary>
/// Representa a configuração de um provedor de IA para uma organização
/// </summary>
public class OrganizationAIProviderConfig : EntityBase
{
    /// <summary>
    /// ID da organização
    /// </summary>
    public int OrganizationId { get; set; }

    /// <summary>
    /// Referência para a organização
    /// </summary>
    public Organization Organization { get; set; }

    /// <summary>
    /// Nome do provedor de IA (OpenAI, Anthropic, etc.)
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Chave de API do provedor (criptografada)
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Modelo selecionado para este provedor
    /// </summary>
    public string SelectedModel { get; set; } = string.Empty;

    /// <summary>
    /// Se esta configuração está habilitada
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Resultado do último teste da API key
    /// </summary>
    public bool? LastTestResult { get; set; }

    /// <summary>
    /// Mensagem do último teste da API key
    /// </summary>
    public string LastTestMessage { get; set; }

    /// <summary>
    /// Data do último teste da API key
    /// </summary>
    public DateTimeOffset? LastTestedAt { get; set; }

    /// <summary>
    /// Limite de tokens por mês para esta configuração (opcional)
    /// </summary>
    public int? MonthlyTokenLimit { get; set; }

    /// <summary>
    /// Tokens utilizados no mês atual
    /// </summary>
    public int TokensUsedThisMonth { get; set; } = 0;

    /// <summary>
    /// Data de reset do contador de tokens
    /// </summary>
    public DateTimeOffset TokenCounterResetAt { get; set; } = DateTimeOffset.UtcNow;
}