using Scriptoryum.Api.Domain.Enums;

namespace Scriptoryum.Api.Domain.Entities;

public class AIProviderConfig : EntityBase
{
    public int AIConfigurationId { get; set; }
    public AIConfiguration AIConfiguration { get; set; }
    
    public AIProvider Provider { get; set; }
    public string ApiKey { get; set; }
    public string SelectedModel { get; set; }
    public bool IsEnabled { get; set; }
    
    // Campos para armazenar informações do último teste da API key
    public bool? LastTestResult { get; set; }
    public string LastTestMessage { get; set; }
    public DateTimeOffset? LastTestedAt { get; set; }
}