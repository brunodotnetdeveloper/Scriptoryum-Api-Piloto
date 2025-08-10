using Microsoft.AspNetCore.Identity;

namespace Scriptoryum.Api.Domain.Entities;

public class ApplicationUser : IdentityUser<string>
{
    public ICollection<Document> Documents { get; set; } = [];
    
    // Configuração de IA do usuário
    public AIConfiguration AIConfiguration { get; set; }
    
    // Sessões de chat do usuário
    public ICollection<ChatSession> ChatSessions { get; set; } = [];
    
    // Notificações do usuário
    public ICollection<Notification> Notifications { get; set; } = [];
}