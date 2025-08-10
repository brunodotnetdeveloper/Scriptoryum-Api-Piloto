using Scriptoryum.Api.Domain.Enums;

namespace Scriptoryum.Api.Domain.Entities;

public class Notification : EntityBase
{
    public string UserId { get; set; }
    public ApplicationUser User { get; set; }
    
    public NotificationType Type { get; set; }
    public NotificationStatus Status { get; set; } = NotificationStatus.Unread;
    
    public string Title { get; set; }
    public string Message { get; set; }
    
    // Referência opcional ao documento relacionado
    public int? DocumentId { get; set; }
    public Document Document { get; set; }
    
    // Dados adicionais em JSON (opcional)
    public string AdditionalData { get; set; }
    
    public DateTimeOffset? ReadAt { get; set; }
    public DateTimeOffset? DismissedAt { get; set; }
}