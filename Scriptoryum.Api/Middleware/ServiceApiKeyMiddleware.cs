using Scriptoryum.Api.Services;
using System.Security.Claims;

namespace Scriptoryum.Api.Middleware;

public class ServiceApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IServiceProvider _serviceProvider;

    public ServiceApiKeyMiddleware(RequestDelegate next, IServiceProvider serviceProvider)
    {
        _next = next;
        _serviceProvider = serviceProvider;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Check if the request has an API key in the Authorization header
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer sk-"))
        {
            var apiKey = authHeader.Substring("Bearer ".Length);
            
            using var scope = _serviceProvider.CreateScope();
            var serviceApiKeyService = scope.ServiceProvider.GetRequiredService<IServiceApiKeyService>();
            
            var serviceApiKeyEntity = await serviceApiKeyService.ValidateApiKeyAsync(apiKey);
            
            if (serviceApiKeyEntity != null)
            {
                // Create claims for the service
                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, serviceApiKeyEntity.CreatedByUserId),
                    new("ServiceApiKeyId", serviceApiKeyEntity.Id.ToString()),
                    new("ServiceName", serviceApiKeyEntity.ServiceName),
                    new("AuthType", "ServiceApiKey")
                };

                // Add permissions if available
                if (!string.IsNullOrEmpty(serviceApiKeyEntity.Permissions))
                {
                    claims.Add(new Claim("Permissions", serviceApiKeyEntity.Permissions));
                }

                var identity = new ClaimsIdentity(claims, "ServiceApiKey");
                context.User = new ClaimsPrincipal(identity);
            }
            else
            {
                // Invalid API key
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Invalid API key");
                return;
            }
        }

        await _next(context);
    }
}