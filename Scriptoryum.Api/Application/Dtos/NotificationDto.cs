using Scriptoryum.Api.Domain.Enums;

namespace Scriptoryum.Api.Application.Dtos;

public class NotificationDto
{
    public int Id { get; set; }
    public NotificationType Type { get; set; }
    public string TypeText { get; set; }
    public NotificationStatus Status { get; set; }
    public string StatusText { get; set; }
    public string Title { get; set; }
    public string Message { get; set; }
    public int? DocumentId { get; set; }
    public string DocumentName { get; set; }
    public string AdditionalData { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime? DismissedAt { get; set; }
}

public class CreateNotificationDto
{
    public string UserId { get; set; }
    public NotificationType Type { get; set; }
    public string Title { get; set; }
    public string Message { get; set; }
    public int? DocumentId { get; set; }
    public string AdditionalData { get; set; }
}

public class UpdateNotificationStatusDto
{
    public NotificationStatus Status { get; set; }
}

public class NotificationSummaryDto
{
    public int TotalCount { get; set; }
    public int UnreadCount { get; set; }
    public List<NotificationDto> RecentNotifications { get; set; } = new();
}