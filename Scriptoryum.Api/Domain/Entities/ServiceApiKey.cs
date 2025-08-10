using Scriptoryum.Api.Domain.Enums;

namespace Scriptoryum.Api.Domain.Entities;

/// <summary>
/// Representa uma chave de API para serviços externos se conectarem com as APIs do Scriptoryum
/// </summary>
public class ServiceApiKey : EntityBase
{
    /// <summary>
    /// Identificador único da chave de API
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Nome descritivo do serviço/aplicação
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Descrição do propósito da chave
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// A chave de API (hash)
    /// </summary>
    public string ApiKeyHash { get; set; } = string.Empty;

    /// <summary>
    /// Prefixo da chave (para identificação, ex: "sk_")
    /// </summary>
    public string KeyPrefix { get; set; } = string.Empty;

    /// <summary>
    /// Últimos 4 caracteres da chave (para exibição)
    /// </summary>
    public string KeySuffix { get; set; } = string.Empty;

    /// <summary>
    /// Status da chave de API
    /// </summary>
    public ServiceApiKeyStatus Status { get; set; } = ServiceApiKeyStatus.Active;

    /// <summary>
    /// Data de expiração da chave (opcional)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Data do último uso da chave
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// Contador de uso da chave
    /// </summary>
    public long UsageCount { get; set; } = 0;

    /// <summary>
    /// Limite de uso por mês (opcional)
    /// </summary>
    public long? MonthlyUsageLimit { get; set; }

    /// <summary>
    /// Uso no mês atual
    /// </summary>
    public long CurrentMonthUsage { get; set; } = 0;

    /// <summary>
    /// Mês/ano do contador atual (formato: YYYY-MM)
    /// </summary>
    public string CurrentMonthYear { get; set; } = DateTime.Now.ToString("yyyy-MM");

    /// <summary>
    /// Permissões da chave (JSON com endpoints permitidos)
    /// </summary>
    public string? Permissions { get; set; }

    /// <summary>
    /// Endereços IP permitidos (JSON array, opcional)
    /// </summary>
    public string? AllowedIPs { get; set; }

    /// <summary>
    /// ID do usuário que criou a chave
    /// </summary>
    public string CreatedByUserId { get; set; } = string.Empty;

    /// <summary>
    /// Usuário que criou a chave
    /// </summary>
    public ApplicationUser? CreatedByUser { get; set; }

    /// <summary>
    /// Verifica se a chave está ativa e não expirou
    /// </summary>
    public bool IsValid => Status == ServiceApiKeyStatus.Active && 
                          (ExpiresAt == null || ExpiresAt > DateTime.UtcNow);

    /// <summary>
    /// Verifica se a chave atingiu o limite mensal
    /// </summary>
    public bool HasReachedMonthlyLimit => MonthlyUsageLimit.HasValue && 
                                         CurrentMonthUsage >= MonthlyUsageLimit.Value;

    /// <summary>
    /// Atualiza o contador de uso
    /// </summary>
    public void IncrementUsage()
    {
        var currentMonth = DateTime.Now.ToString("yyyy-MM");
        
        // Reset contador se mudou o mês
        if (CurrentMonthYear != currentMonth)
        {
            CurrentMonthYear = currentMonth;
            CurrentMonthUsage = 0;
        }
        
        UsageCount++;
        CurrentMonthUsage++;
        LastUsedAt = DateTime.UtcNow;
    }
}