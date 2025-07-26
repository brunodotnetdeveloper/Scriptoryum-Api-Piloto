using Scriptoryum.Api.Domain.Enums;

namespace Scriptoryum.Api.Domain.Entities;

public class ChatMessage : EntityBase
{
    public int ChatSessionId { get; set; }
    public ChatSession ChatSession { get; set; }
    
    public MessageRole Role { get; set; }
    public string Content { get; set; }
    
    // Documento de contexto para a mensagem (opcional)
    public int? DocumentId { get; set; }
    public Document Document { get; set; }
    
    // Metadados da mensagem
    public string DocumentName { get; set; } // Nome do documento no momento da mensagem
    public int? TokenCount { get; set; } // Número de tokens da mensagem
    public decimal? Cost { get; set; } // Custo da mensagem (para mensagens do assistente)
    
    // Informações do modelo usado (para mensagens do assistente)
    public AIProvider? AIProvider { get; set; }
    public string ModelUsed { get; set; }
    
    // Tempo de resposta (para mensagens do assistente)
    public int? ResponseTimeMs { get; set; }
}