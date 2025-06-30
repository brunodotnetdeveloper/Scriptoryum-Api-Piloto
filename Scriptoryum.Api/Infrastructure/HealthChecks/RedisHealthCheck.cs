using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace Scriptoryum.Api.Infrastructure.HealthChecks;

public class RedisHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly ILogger<RedisHealthCheck> _logger;

    public RedisHealthCheck(IConnectionMultiplexer connectionMultiplexer, ILogger<RedisHealthCheck> logger)
    {
        _connectionMultiplexer = connectionMultiplexer;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_connectionMultiplexer == null)
            {
                _logger.LogInformation("Redis ConnectionMultiplexer is null - application running without Redis");
                return HealthCheckResult.Degraded("Redis is not configured - application running in degraded mode without queue functionality");
            }

            if (!_connectionMultiplexer.IsConnected)
            {
                _logger.LogWarning("Redis is not connected");
                return HealthCheckResult.Degraded("Redis connection is not established - queue functionality unavailable");
            }

            // Test actual Redis connectivity with a simple ping
            var database = _connectionMultiplexer.GetDatabase();
            var pingResult = await database.PingAsync();
            
            _logger.LogDebug("Redis ping successful: {PingTime}ms", pingResult.TotalMilliseconds);
            
            return HealthCheckResult.Healthy($"Redis is healthy (ping: {pingResult.TotalMilliseconds:F2}ms)");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis health check failed - application will continue without Redis");
            return HealthCheckResult.Degraded($"Redis health check failed: {ex.Message} - queue functionality unavailable", ex);
        }
    }
}