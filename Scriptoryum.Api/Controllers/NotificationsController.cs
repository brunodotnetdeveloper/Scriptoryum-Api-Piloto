using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Scriptoryum.Api.Application.Dtos;
using Scriptoryum.Api.Application.Services;
using Scriptoryum.Api.Domain.Enums;
using System.Security.Claims;

namespace Scriptoryum.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController(INotificationService notificationService) : ControllerBase
{

    /// <summary>
    /// Obtém as notificações do usuário
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<NotificationDto>>> GetNotifications(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] NotificationStatus? status = null)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var notifications = await notificationService.GetUserNotificationsAsync(userId, page, pageSize, status);
        return Ok(notifications);
    }

    /// <summary>
    /// Obtém resumo das notificações do usuário
    /// </summary>
    [HttpGet("summary")]
    public async Task<ActionResult<NotificationSummaryDto>> GetNotificationSummary()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var summary = await notificationService.GetUserNotificationSummaryAsync(userId);
        return Ok(summary);
    }

    /// <summary>
    /// Obtém contagem de notificações não lidas
    /// </summary>
    [HttpGet("unread-count")]
    public async Task<ActionResult<int>> GetUnreadCount()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var count = await notificationService.GetUnreadCountAsync(userId);
        return Ok(count);
    }

    /// <summary>
    /// Cria uma nova notificação
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<NotificationDto>> CreateNotification([FromBody] CreateNotificationDto createDto)
    {
        if (createDto == null)
            return BadRequest("Dados inválidos");

        if (string.IsNullOrWhiteSpace(createDto.UserId) || string.IsNullOrWhiteSpace(createDto.Title) || string.IsNullOrWhiteSpace(createDto.Message))
            return BadRequest("UserId, Title e Message são obrigatórios");

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var authType = User.FindFirst("AuthType")?.Value;
        if (authType == "ServiceApiKey")
        {
            var permissions = User.FindFirst("Permissions")?.Value ?? string.Empty;
            var hasPermission = permissions
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Any(p => string.Equals(p, "notifications:create", StringComparison.OrdinalIgnoreCase));

            if (!hasPermission)
                return Forbid();

            if (!string.Equals(createDto.UserId, userId, StringComparison.Ordinal))
                return Forbid();
        }
        else
        {
            // Usuário autenticado via JWT só pode criar notificações para si mesmo
            if (!string.Equals(createDto.UserId, userId, StringComparison.Ordinal))
                return Forbid();
        }

        try
        {
            var created = await notificationService.CreateNotificationAsync(createDto);
            return Created($"/api/notifications/{created.Id}", created);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Erro ao criar notificação", error = ex.Message });
        }
    }

    /// <summary>
    /// Atualiza o status de uma notificação
    /// </summary>
    [HttpPut("{id}/status")]
    public async Task<ActionResult<NotificationDto>> UpdateNotificationStatus(
        int id,
        [FromBody] UpdateNotificationStatusDto updateDto)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        try
        {
            var notification = await notificationService.UpdateNotificationStatusAsync(id, userId, updateDto.Status);
            return Ok(notification);
        }
        catch (ArgumentException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Marca todas as notificações como lidas
    /// </summary>
    [HttpPost("mark-all-read")]
    public async Task<ActionResult> MarkAllAsRead()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        await notificationService.MarkAllAsReadAsync(userId);
        return Ok();
    }

    /// <summary>
    /// Deleta uma notificação
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteNotification(int id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var success = await notificationService.DeleteNotificationAsync(id, userId);
        if (!success)
            return NotFound();

        return NoContent();
    }
}