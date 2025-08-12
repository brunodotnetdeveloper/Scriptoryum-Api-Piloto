using Microsoft.EntityFrameworkCore;
using Scriptoryum.Api.Application.Dtos;
using Scriptoryum.Api.Domain.Entities;
using Scriptoryum.Api.Domain.Enums;
using Scriptoryum.Api.Infrastructure.Context;

namespace Scriptoryum.Api.Application.Services;

public interface INotificationService
{
    Task<NotificationDto> CreateNotificationAsync(CreateNotificationDto createDto);
    Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(string userId, int page = 1, int pageSize = 20, NotificationStatus? status = null);
    Task<NotificationSummaryDto> GetUserNotificationSummaryAsync(string userId);
    Task<NotificationDto> UpdateNotificationStatusAsync(int notificationId, string userId, NotificationStatus status);
    Task<bool> MarkAllAsReadAsync(string userId);
    Task<bool> DeleteNotificationAsync(int notificationId, string userId);
    Task<int> GetUnreadCountAsync(string userId);
}

public class NotificationService : INotificationService
{
    private readonly ScriptoryumDbContext _context;

    public NotificationService(ScriptoryumDbContext context)
    {
        _context = context;
    }

    public async Task<NotificationDto> CreateNotificationAsync(CreateNotificationDto createDto)
    {
        var notification = new Notification
        {
            UserId = createDto.UserId,
            Type = createDto.Type,
            Title = createDto.Title,
            Message = createDto.Message,
            DocumentId = createDto.DocumentId,
            AdditionalData = createDto.AdditionalData,
            Status = NotificationStatus.Unread.ToString()
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        return await GetNotificationDtoAsync(notification.Id);
    }

    public async Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(string userId, int page = 1, int pageSize = 20, NotificationStatus? status = null)
    {
        var query = _context.Notifications
            .Include(n => n.Document)
            .Where(n => n.UserId == userId);

        if (status.HasValue)
        {
            query = query.Where(n => n.Status == status.Value.ToString());
        }

        var notifications = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return notifications.Select(MapToDto);
    }

    public async Task<NotificationSummaryDto> GetUserNotificationSummaryAsync(string userId)
    {
        var totalCount = await _context.Notifications
            .Where(n => n.UserId == userId)
            .CountAsync();

        var unreadCount = await _context.Notifications
            .Where(n => n.UserId == userId && n.Status == NotificationStatus.Unread.ToString())
            .CountAsync();

        var recentNotifications = await _context.Notifications
            .Include(n => n.Document)
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(5)
            .ToListAsync();

        return new NotificationSummaryDto
        {
            TotalCount = totalCount,
            UnreadCount = unreadCount,
            RecentNotifications = recentNotifications.Select(MapToDto).ToList()
        };
    }

    public async Task<NotificationDto> UpdateNotificationStatusAsync(int notificationId, string userId, NotificationStatus status)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

        if (notification == null)
            throw new ArgumentException("Notification not found or access denied");

        notification.Status = status.ToString();
        
        if (status == NotificationStatus.Read && notification.ReadAt == null)
        {
            notification.ReadAt = DateTime.UtcNow;
        }
        else if (status == NotificationStatus.Dismissed && notification.DismissedAt == null)
        {
            notification.DismissedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return await GetNotificationDtoAsync(notificationId);
    }

    public async Task<bool> MarkAllAsReadAsync(string userId)
    {
        var unreadNotifications = await _context.Notifications
            .Where(n => n.UserId == userId && n.Status == NotificationStatus.Unread.ToString())
            .ToListAsync();

        foreach (var notification in unreadNotifications)
        {
            notification.Status = NotificationStatus.Read.ToString();
            notification.ReadAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteNotificationAsync(int notificationId, string userId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

        if (notification == null)
            return false;

        _context.Notifications.Remove(notification);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<int> GetUnreadCountAsync(string userId)
    {
        return await _context.Notifications
            .Where(n => n.UserId == userId && n.Status == NotificationStatus.Unread.ToString())
            .CountAsync();
    }

    private async Task<NotificationDto> GetNotificationDtoAsync(int notificationId)
    {
        var notification = await _context.Notifications
            .Include(n => n.Document)
            .FirstOrDefaultAsync(n => n.Id == notificationId);

        return MapToDto(notification);
    }

    private static NotificationDto MapToDto(Notification notification)
    {
        return new NotificationDto
        {
            Id = notification.Id,
            Type = notification.Type.ToString(),
            TypeText = notification.Type.ToString(),
            Status = notification.Status.ToString(),
            StatusText = notification.Status.ToString(),
            Title = notification.Title,
            Message = notification.Message,
            DocumentId = notification.DocumentId,
            DocumentName = notification.Document?.OriginalFileName,
            AdditionalData = notification.AdditionalData,
            CreatedAt = notification.CreatedAt,
            ReadAt = notification.ReadAt,
            DismissedAt = notification.DismissedAt
        };
    }
}