using Microsoft.AspNetCore.Identity;

namespace Scriptoryum.Api.Domain.Entities;

public class ApplicationUser : IdentityUser<string>
{
    public ICollection<Document> Documents { get; set; } = new List<Document>();
    
    // Configuração de IA do usuário
    public AIConfiguration AIConfiguration { get; set; }
    
    // Sessões de chat do usuário
    public ICollection<ChatSession> ChatSessions { get; set; } = new List<ChatSession>();
}