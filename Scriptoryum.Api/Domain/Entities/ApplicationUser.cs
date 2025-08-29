using Microsoft.AspNetCore.Identity;
using Scriptoryum.Api.Domain.Enums;

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
    
    // Organização à qual o usuário pertence
    public int? OrganizationId { get; set; }
    public Organization Organization { get; set; }
    
    // Papel do usuário na organização
    public OrganizationRole Role { get; set; } = OrganizationRole.Member;
    
    // Status do usuário na organização
    public OrganizationUserStatus Status { get; set; } = OrganizationUserStatus.Active;
    
    // Data em que o usuário foi adicionado à organização
    public DateTimeOffset? JoinedAt { get; set; }
    
    // Data em que o usuário foi removido da organização (se aplicável)
    public DateTimeOffset? RemovedAt { get; set; }
    
    // Workspaces aos quais o usuário tem acesso
    public ICollection<WorkspaceUser> WorkspaceUsers { get; set; } = [];
}