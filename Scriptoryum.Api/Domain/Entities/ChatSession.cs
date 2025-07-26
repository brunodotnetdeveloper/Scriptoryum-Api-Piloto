namespace Scriptoryum.Api.Domain.Entities;

public class ChatSession : EntityBase
{
    public string UserId { get; set; }
    public ApplicationUser User { get; set; }
    
    public string Title { get; set; }
    public string Description { get; set; }
    
    // Documento associado à sessão (opcional)
    public int? DocumentId { get; set; }
    public Document Document { get; set; }
    
    // Metadados da sessão
    public int MessageCount { get; set; } = 0;
    public DateTimeOffset LastActivityAt { get; set; } = DateTimeOffset.UtcNow;
    
    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}