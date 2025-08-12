using Scriptoryum.Api.Services;
using System.Security.Claims;
using System.Text.Json.Serialization;

namespace Scriptoryum.Api.Middleware;

public class ServiceApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ServiceApiKeyMiddleware> _logger;

    public ServiceApiKeyMiddleware(RequestDelegate next, IServiceProvider serviceProvider, ILogger<ServiceApiKeyMiddleware> logger)
    {
        _next = next;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();

        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer sk-"))
        {
            var apiKey = authHeader.Substring("Bearer ".Length);

            _logger.LogInformation("Tentando autenticar usando ServiceApiKey: {ApiKeyPrefix}...", apiKey.Substring(0, Math.Min(8, apiKey.Length)));

            using var scope = _serviceProvider.CreateScope();
            var serviceApiKeyService = scope.ServiceProvider.GetRequiredService<IServiceApiKeyService>();

            var serviceApiKeyEntity = await serviceApiKeyService.ValidateApiKeyAsync(apiKey);

            var serviceApiKeyEntityJson = System.Text.Json.JsonSerializer.Serialize(serviceApiKeyEntity);
            _logger.LogInformation("serviceApiKeyEntity JSON: {ServiceApiKeyEntityJson}", serviceApiKeyEntityJson);
            

            if (serviceApiKeyEntity != null)
            {
                _logger.LogInformation("ServiceApiKey autenticado com sucesso. ServiceName: {ServiceName}, UserId: {UserId}", serviceApiKeyEntity.ServiceName, serviceApiKeyEntity.CreatedByUserId);

                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, serviceApiKeyEntity.CreatedByUserId),
                    new("ServiceApiKeyId", serviceApiKeyEntity.Id.ToString()),
                    new("ServiceName", serviceApiKeyEntity.ServiceName),
                    new("AuthType", "ServiceApiKey")
                };

                if (!string.IsNullOrEmpty(serviceApiKeyEntity.Permissions))
                {
                    claims.Add(new Claim("Permissions", serviceApiKeyEntity.Permissions));
                }

                var identity = new ClaimsIdentity(claims, "ServiceApiKey");
                context.User = new ClaimsPrincipal(identity);
            }
            else
            {
                _logger.LogWarning("Tentativa de autenticação falhou: API key inválida.");
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Invalid API key");
                return;
            }
        }

        await _next(context);
    }
}