using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Scriptoryum.Api.Services;
using System.Security.Claims;

namespace Scriptoryum.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ServiceApiKeyController : ControllerBase
{
    private readonly IServiceApiKeyService _serviceApiKeyService;

    public ServiceApiKeyController(IServiceApiKeyService serviceApiKeyService)
    {
        _serviceApiKeyService = serviceApiKeyService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateApiKey([FromBody] CreateApiKeyRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        try
        {
            var (apiKey, plainKey) = await _serviceApiKeyService.CreateApiKeyAsync(
                request.ServiceName,
                request.Description,
                request.MonthlyUsageLimit,
                request.ExpiresAt,
                request.Permissions,
                request.AllowedIPs,
                userId
            );

            return Ok(new CreateApiKeyResponse
            {
                Id = apiKey.Id,
                ServiceName = apiKey.ServiceName,
                Description = apiKey.Description,
                ApiKey = plainKey, // Only returned once during creation
                KeyPrefix = apiKey.KeyPrefix,
                KeySuffix = apiKey.KeySuffix,
                Status = apiKey.Status.ToString(),
                ExpiresAt = apiKey.ExpiresAt,
                MonthlyUsageLimit = apiKey.MonthlyUsageLimit,
                Permissions = apiKey.Permissions,
                AllowedIPs = apiKey.AllowedIPs,
                CreatedAt = apiKey.CreatedAt
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Erro ao criar API key", error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetApiKeys()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var apiKeys = await _serviceApiKeyService.GetApiKeysAsync(userId);

        var response = apiKeys.Select(ak => new ApiKeyResponse
        {
            Id = ak.Id,
            ServiceName = ak.ServiceName,
            Description = ak.Description,
            KeyPrefix = ak.KeyPrefix,
            KeySuffix = ak.KeySuffix,
            Status = ak.Status.ToString(),
            ExpiresAt = ak.ExpiresAt,
            LastUsedAt = ak.LastUsedAt,
            UsageCount = ak.UsageCount,
            MonthlyUsageLimit = ak.MonthlyUsageLimit,
            CurrentMonthUsage = ak.CurrentMonthUsage,
            Permissions = ak.Permissions,
            AllowedIPs = ak.AllowedIPs,
            CreatedAt = ak.CreatedAt
        }).ToList();

        return Ok(response);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetApiKey(int id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var apiKey = await _serviceApiKeyService.GetApiKeyByIdAsync(id, userId);
        if (apiKey == null)
            return NotFound();

        var response = new ApiKeyResponse
        {
            Id = apiKey.Id,
            ServiceName = apiKey.ServiceName,
            Description = apiKey.Description,
            KeyPrefix = apiKey.KeyPrefix,
            KeySuffix = apiKey.KeySuffix,
            Status = apiKey.Status.ToString(),
            ExpiresAt = apiKey.ExpiresAt,
            LastUsedAt = apiKey.LastUsedAt,
            UsageCount = apiKey.UsageCount,
            MonthlyUsageLimit = apiKey.MonthlyUsageLimit,
            CurrentMonthUsage = apiKey.CurrentMonthUsage,
            Permissions = apiKey.Permissions,
            AllowedIPs = apiKey.AllowedIPs,
            CreatedAt = apiKey.CreatedAt
        };

        return Ok(response);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateApiKey(int id, [FromBody] UpdateApiKeyRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var success = await _serviceApiKeyService.UpdateApiKeyAsync(
            id,
            request.ServiceName,
            request.Description,
            request.MonthlyUsageLimit,
            request.ExpiresAt,
            request.Permissions,
            request.AllowedIPs,
            userId
        );

        if (!success)
            return NotFound();

        return Ok(new { message = "API key atualizada com sucesso" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> RevokeApiKey(int id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var success = await _serviceApiKeyService.RevokeApiKeyAsync(id, userId);
        if (!success)
            return NotFound();

        return Ok(new { message = "API key revogada com sucesso" });
    }
}

public class CreateApiKeyRequest
{
    public string ServiceName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? MonthlyUsageLimit { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? Permissions { get; set; }
    public string? AllowedIPs { get; set; }
}

public class UpdateApiKeyRequest
{
    public string ServiceName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? MonthlyUsageLimit { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? Permissions { get; set; }
    public string? AllowedIPs { get; set; }
}

public class CreateApiKeyResponse
{
    public int Id { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ApiKey { get; set; } = string.Empty; // Only returned during creation
    public string KeyPrefix { get; set; } = string.Empty;
    public string KeySuffix { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset? ExpiresAt { get; set; }
    public long? MonthlyUsageLimit { get; set; }
    public string? Permissions { get; set; }
    public string? AllowedIPs { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class ApiKeyResponse
{
    public int Id { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string KeyPrefix { get; set; } = string.Empty;
    public string KeySuffix { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset? LastUsedAt { get; set; }
    public long UsageCount { get; set; }
    public long? MonthlyUsageLimit { get; set; }
    public long CurrentMonthUsage { get; set; }
    public string? Permissions { get; set; }
    public string? AllowedIPs { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}