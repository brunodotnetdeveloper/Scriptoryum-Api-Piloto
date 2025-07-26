using System.ComponentModel.DataAnnotations;
using Scriptoryum.Api.Domain.Enums;

namespace Scriptoryum.Api.Application.Dtos;

public class AIConfigurationDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public AIProvider DefaultProvider { get; set; }
    public List<AIProviderConfigDto> Providers { get; set; } = new();
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public class AIProviderConfigDto
{
    public int Id { get; set; }
    public AIProvider Provider { get; set; }
    
    [Required(ErrorMessage = "API Key é obrigatória")]
    public string ApiKey { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Modelo selecionado é obrigatório")]
    public string SelectedModel { get; set; } = string.Empty;
    
    public bool IsEnabled { get; set; }
    public bool? LastTestResult { get; set; }
    public string LastTestMessage { get; set; }
    public DateTimeOffset? LastTestedAt { get; set; }
}

public class AIModelDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int MaxTokens { get; set; }
    public decimal CostPer1kTokens { get; set; }
}

public class UpdateAIConfigurationDto
{
    [Required(ErrorMessage = "Provedor padrão é obrigatório")]
    public AIProvider DefaultProvider { get; set; }
    
    [Required(ErrorMessage = "Configurações de provedores são obrigatórias")]
    public List<AIProviderConfigDto> Providers { get; set; } = new();
}

public class TestApiKeyDto
{
    [Required(ErrorMessage = "Provedor é obrigatório")]
    public AIProvider Provider { get; set; }
    
    [Required(ErrorMessage = "API Key é obrigatória")]
    public string ApiKey { get; set; } = string.Empty;
}

public class AIConfigurationResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public AIConfigurationDto? Configuration { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class TestApiKeyResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();
}