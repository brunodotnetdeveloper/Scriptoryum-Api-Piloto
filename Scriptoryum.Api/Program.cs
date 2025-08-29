using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Scriptoryum.Api.Application.Services;
using Scriptoryum.Api.Domain.Entities;
using Scriptoryum.Api.Infrastructure.Clients;
using Scriptoryum.Api.Infrastructure.Configuration;
using Scriptoryum.Api.Infrastructure.Context;
using Scriptoryum.Api.Infrastructure.HealthChecks;
using Scriptoryum.Api.Infrastructure.Services;
using Scriptoryum.Api.Middleware;
using StackExchange.Redis;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configure PostgreSQL DbContext
builder.Services.AddDbContext<ScriptoryumDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"), o => 
    {
        o.UseVector();
        o.CommandTimeout(300); // 5 minutes timeout for migrations
    }));

// Configure Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 1;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.AllowedUserNameCharacters =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ScriptoryumDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddControllers();

// Register application services
builder.Services.AddScoped<IDocumentsService, DocumentsService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IApiKeyService, ApiKeyService>();
builder.Services.AddScoped<IOrganizationService, OrganizationService>();
builder.Services.AddScoped<IWorkspaceService, WorkspaceService>();

builder.Services.AddScoped<IAuthService, AuthService>();

// Register AI Configuration and Chat services
builder.Services.AddScoped<IAIConfigService, AIConfigService>();
builder.Services.AddScoped<IAIService, AIService>();
builder.Services.AddScoped<IRagService, RagService>();
builder.Services.AddScoped<IEscribaService, EscribaService>();

// Configure Cloudflare R2 options
builder.Services.Configure<CloudflareR2Options>(builder.Configuration.GetSection(CloudflareR2Options.SectionName));

// Register Cloudflare R2 client
builder.Services.AddScoped<ICloudflareR2Client, CloudflareR2Client>();

// Configure Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(provider =>
{
    var connectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
    var configurationOptions = ConfigurationOptions.Parse(connectionString);

    // Configure resilient connection settings
    configurationOptions.AbortOnConnectFail = false;
    configurationOptions.ConnectRetry = 3;
    configurationOptions.ConnectTimeout = 5000;
    configurationOptions.SyncTimeout = 5000;

    var logger = provider.GetRequiredService<ILogger<Program>>();

    try
    {
        var multiplexer = ConnectionMultiplexer.Connect(configurationOptions);

        // Log connection events
        multiplexer.ConnectionFailed += (sender, e) =>
        {
            logger.LogError("Redis connection failed: {Exception}", e.Exception?.Message);
        };

        multiplexer.ConnectionRestored += (sender, e) =>
        {
            logger.LogInformation("Redis connection restored");
        };

        logger.LogInformation("Redis connection established successfully");
        return multiplexer;
    }
    catch (Exception ex)
    {
        logger.LogWarning("Failed to connect to Redis: {Message}. Application will continue without Redis.", ex.Message);

        // Return a null multiplexer that can be handled gracefully
        return null;
    }
});

// Register Redis Queue Service
builder.Services.AddScoped<IRedisQueueService, RedisQueueService>();

// Add Health Checks
builder.Services.AddHealthChecks()
    .AddCheck<RedisHealthCheck>("redis");

// Register Redis Health Check with proper dependency injection
builder.Services.AddSingleton<RedisHealthCheck>(provider =>
{
    var connectionMultiplexer = provider.GetService<IConnectionMultiplexer>();
    var logger = provider.GetRequiredService<ILogger<RedisHealthCheck>>();
    return new RedisHealthCheck(connectionMultiplexer, logger);
});

builder.Services.AddHttpClient();

// Configure JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
            {
                context.Response.Headers["Token-Expired"] = "true";
            }
            return Task.CompletedTask;
        }
    };
});

// Add CORS configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowScriptoryumOrigins", policy =>
    {
        policy.WithOrigins(
                "https://app.scriptoryum.com.br",
                "http://localhost:8080",
                "https://localhost:8080",
                "http://localhost:5173",
                "https://localhost:5173"
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Configure Swagger/OpenAPI
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Scriptoryum API",
        Version = "v1",
        Description = "API para gerenciamento de documentos e análise de dados do Scriptoryum",
        Contact = new OpenApiContact
        {
            Name = "Scriptoryum Team",
            Email = "contato@scriptoryum.com"
        }
    });

    // Enable annotations for better documentation
    c.EnableAnnotations();

    // Configure JWT Authentication for Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header usando o esquema Bearer. \r\n\r\n" +
                      "Digite 'Bearer' [espaço] e então seu token na entrada de texto abaixo.\r\n\r\n" +
                      "Exemplo: \"Bearer 12345abcdef\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    // Configure API Key Authentication for Swagger
    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description = "API Key Authorization header usando o esquema Bearer com prefixo 'sk-'. \r\n\r\n" +
                      "Digite 'Bearer' [espaço] e então sua API key na entrada de texto abaixo.\r\n\r\n" +
                      "Exemplo: \"Bearer sk-1234567890abcdef\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header,
            },
            new List<string>()
        },
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                },
                Scheme = "ApiKey",
                Name = "Authorization",
                In = ParameterLocation.Header,
            },
            new List<string>()
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Scriptoryum API V1");
    c.RoutePrefix = "swagger";
});
//}

app.UseHttpsRedirection();

// Use CORS before authentication/authorization
app.UseCors("AllowScriptoryumOrigins");

// Add Service API Key middleware before authentication
app.UseMiddleware<ServiceApiKeyMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Map health check endpoint
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/redis", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Name == "redis"
});

app.Run();
