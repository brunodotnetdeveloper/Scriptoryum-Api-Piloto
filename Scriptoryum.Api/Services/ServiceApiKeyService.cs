using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Scriptoryum.Api.Domain.Entities;
using Scriptoryum.Api.Domain.Enums;
using Scriptoryum.Api.Infrastructure.Context;

namespace Scriptoryum.Api.Services;

public interface IServiceApiKeyService
{
    Task<(ServiceApiKey apiKey, string plainKey)> CreateApiKeyAsync(string serviceName, string description, 
        int? monthlyUsageLimit, DateTime? expiresAt, string? permissions, string? allowedIPs, string createdByUserId);
    Task<ServiceApiKey?> ValidateApiKeyAsync(string apiKey);
    Task<bool> RevokeApiKeyAsync(int id, string userId);
    Task<List<ServiceApiKey>> GetApiKeysAsync(string userId);
    Task<ServiceApiKey?> GetApiKeyByIdAsync(int id, string userId);
    Task<bool> UpdateApiKeyAsync(int id, string serviceName, string description, 
        int? monthlyUsageLimit, DateTime? expiresAt, string? permissions, string? allowedIPs, string userId);
}

public class ServiceApiKeyService : IServiceApiKeyService
{
    private readonly ScriptoryumDbContext _context;
    private const string KeyPrefix = "sk-";
    private const int KeyLength = 48; // Total length including prefix

    public ServiceApiKeyService(ScriptoryumDbContext context)
    {
        _context = context;
    }

    public async Task<(ServiceApiKey apiKey, string plainKey)> CreateApiKeyAsync(string serviceName, string description,
        int? monthlyUsageLimit, DateTime? expiresAt, string? permissions, string? allowedIPs, string createdByUserId)
    {
        // Generate a secure random API key
        var plainKey = GenerateApiKey();
        var keyHash = HashApiKey(plainKey);
        var keyPrefix = plainKey.Substring(0, Math.Min(10, plainKey.Length));
        var keySuffix = plainKey.Length > 10 ? plainKey.Substring(plainKey.Length - 4) : "";

        var apiKey = new ServiceApiKey
        {
            ServiceName = serviceName,
            Description = description,
            ApiKeyHash = keyHash,
            KeyPrefix = keyPrefix,
            KeySuffix = keySuffix,
            Status = ServiceApiKeyStatus.Active,
            ExpiresAt = expiresAt,
            MonthlyUsageLimit = monthlyUsageLimit,
            CurrentMonthUsage = 0,
            CurrentMonthYear = DateTime.UtcNow.ToString("yyyy-MM"),
            Permissions = permissions,
            AllowedIPs = allowedIPs,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.ServiceApiKeys.Add(apiKey);
        await _context.SaveChangesAsync();

        return (apiKey, plainKey);
    }

    public async Task<ServiceApiKey?> ValidateApiKeyAsync(string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey))
            return null;

        var keyHash = HashApiKey(apiKey);
        
        var serviceApiKey = await _context.ServiceApiKeys
            .Include(sak => sak.CreatedByUser)
            .FirstOrDefaultAsync(sak => sak.ApiKeyHash == keyHash);

        if (serviceApiKey == null)
            return null;

        // Check if key is valid
        if (!serviceApiKey.IsValid)
            return null;

        // Update usage statistics
        serviceApiKey.IncrementUsage();
        serviceApiKey.LastUsedAt = DateTime.UtcNow;
        serviceApiKey.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return serviceApiKey;
    }

    public async Task<bool> RevokeApiKeyAsync(int id, string userId)
    {
        var apiKey = await _context.ServiceApiKeys
            .FirstOrDefaultAsync(sak => sak.Id == id && sak.CreatedByUserId == userId);

        if (apiKey == null)
            return false;

        apiKey.Status = ServiceApiKeyStatus.Revoked;
        apiKey.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<ServiceApiKey>> GetApiKeysAsync(string userId)
    {
        return await _context.ServiceApiKeys
            .Where(sak => sak.CreatedByUserId == userId)
            .OrderByDescending(sak => sak.CreatedAt)
            .ToListAsync();
    }

    public async Task<ServiceApiKey?> GetApiKeyByIdAsync(int id, string userId)
    {
        return await _context.ServiceApiKeys
            .FirstOrDefaultAsync(sak => sak.Id == id && sak.CreatedByUserId == userId);
    }

    public async Task<bool> UpdateApiKeyAsync(int id, string serviceName, string description,
        int? monthlyUsageLimit, DateTime? expiresAt, string? permissions, string? allowedIPs, string userId)
    {
        var apiKey = await _context.ServiceApiKeys
            .FirstOrDefaultAsync(sak => sak.Id == id && sak.CreatedByUserId == userId);

        if (apiKey == null)
            return false;

        apiKey.ServiceName = serviceName;
        apiKey.Description = description;
        apiKey.MonthlyUsageLimit = monthlyUsageLimit;
        apiKey.ExpiresAt = expiresAt;
        apiKey.Permissions = permissions;
        apiKey.AllowedIPs = allowedIPs;
        apiKey.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    private static string GenerateApiKey()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[32]; // 256 bits
        rng.GetBytes(bytes);
        
        var base64 = Convert.ToBase64String(bytes)
            .Replace("+", "")
            .Replace("/", "")
            .Replace("=", "");
        
        return $"{KeyPrefix}{base64}";
    }

    private static string HashApiKey(string apiKey)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(apiKey));
        return Convert.ToBase64String(hashedBytes);
    }
}