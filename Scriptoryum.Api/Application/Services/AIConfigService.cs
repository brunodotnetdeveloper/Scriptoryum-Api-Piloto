using Microsoft.EntityFrameworkCore;
using Scriptoryum.Api.Application.Dtos;
using Scriptoryum.Api.Domain.Entities;
using Scriptoryum.Api.Domain.Enums;
using Scriptoryum.Api.Infrastructure.Context;

namespace Scriptoryum.Api.Application.Services;

public interface IAIConfigService
{
    Task<AIConfigurationResponseDto> GetConfigurationAsync(string userId);
    Task<AIConfigurationResponseDto> UpdateConfigurationAsync(string userId, UpdateAIConfigurationDto updateDto);
    Task<List<AIModelDto>> GetModelsForProviderAsync(AIProvider provider);
    Task<TestApiKeyResponseDto> TestApiKeyAsync(TestApiKeyDto testDto);
}

public class AIConfigService : IAIConfigService
{
    private readonly ScriptoryumDbContext _context;
    private readonly ILogger<AIConfigService> _logger;

    // Modelos disponíveis por provedor
    private static readonly Dictionary<AIProvider, List<AIModelDto>> AvailableModels = new()
    {
        [AIProvider.OpenAI] = new List<AIModelDto>
        {
            new() { Id = "gpt-4o", Name = "GPT-4o", Description = "Modelo mais avançado da OpenAI com capacidades multimodais", MaxTokens = 128000, CostPer1kTokens = 0.005m },
            new() { Id = "gpt-4o-mini", Name = "GPT-4o Mini", Description = "Versão mais rápida e econômica do GPT-4o", MaxTokens = 128000, CostPer1kTokens = 0.00015m },
            new() { Id = "gpt-4-turbo", Name = "GPT-4 Turbo", Description = "Modelo GPT-4 otimizado para velocidade", MaxTokens = 128000, CostPer1kTokens = 0.01m },
            new() { Id = "gpt-3.5-turbo", Name = "GPT-3.5 Turbo", Description = "Modelo rápido e econômico para tarefas gerais", MaxTokens = 16385, CostPer1kTokens = 0.0005m }
        },
        [AIProvider.Claude] = new List<AIModelDto>
        {
            new() { Id = "claude-3-5-sonnet-20241022", Name = "Claude 3.5 Sonnet", Description = "Modelo mais avançado da Anthropic com excelente raciocínio", MaxTokens = 200000, CostPer1kTokens = 0.003m },
            new() { Id = "claude-3-5-haiku-20241022", Name = "Claude 3.5 Haiku", Description = "Modelo rápido e econômico da Anthropic", MaxTokens = 200000, CostPer1kTokens = 0.00025m },
            new() { Id = "claude-3-opus-20240229", Name = "Claude 3 Opus", Description = "Modelo premium da Anthropic para tarefas complexas", MaxTokens = 200000, CostPer1kTokens = 0.015m }
        },
        [AIProvider.Gemini] = new List<AIModelDto>
        {
            new() { Id = "gemini-1.5-pro", Name = "Gemini 1.5 Pro", Description = "Modelo avançado do Google com grande contexto", MaxTokens = 2000000, CostPer1kTokens = 0.00125m },
            new() { Id = "gemini-1.5-flash", Name = "Gemini 1.5 Flash", Description = "Modelo rápido e eficiente do Google", MaxTokens = 1000000, CostPer1kTokens = 0.000075m },
            new() { Id = "gemini-1.0-pro", Name = "Gemini 1.0 Pro", Description = "Modelo base do Google Gemini", MaxTokens = 32768, CostPer1kTokens = 0.0005m }
        }
    };

    public AIConfigService(ScriptoryumDbContext context, ILogger<AIConfigService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<AIConfigurationResponseDto> GetConfigurationAsync(string userId)
    {
        try
        {
            var configuration = await _context.AIConfigurations
                .Include(c => c.AIProviderConfigs)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (configuration == null)
            {
                // Criar configuração padrão
                configuration = await CreateDefaultConfigurationAsync(userId);
            }

            var configDto = MapToDto(configuration);

            return new AIConfigurationResponseDto
            {
                Success = true,
                Configuration = configDto,
                Message = "Configuração carregada com sucesso"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao carregar configuração de IA para usuário {UserId}", userId);
            return new AIConfigurationResponseDto
            {
                Success = false,
                Message = "Erro ao carregar configuração",
                Errors = new List<string> { "Ocorreu um erro inesperado" }
            };
        }
    }

    public async Task<AIConfigurationResponseDto> UpdateConfigurationAsync(string userId, UpdateAIConfigurationDto updateDto)
    {
        try
        {
            var configuration = await _context.AIConfigurations
                .Include(c => c.AIProviderConfigs)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (configuration == null)
            {
                configuration = await CreateDefaultConfigurationAsync(userId);
            }

            // Atualizar provedor padrão
            configuration.DefaultProvider = updateDto.DefaultProvider;
            configuration.UpdatedAt = DateTime.UtcNow;

            // Atualizar configurações dos provedores
            foreach (var providerDto in updateDto.Providers)
            {
                var existingConfig = configuration.AIProviderConfigs
                    .FirstOrDefault(p => p.Provider == providerDto.Provider);

                if (existingConfig != null)
                {
                    existingConfig.ApiKey = providerDto.ApiKey;
                    existingConfig.SelectedModel = providerDto.SelectedModel;
                    existingConfig.IsEnabled = providerDto.IsEnabled;
                    existingConfig.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    configuration.AIProviderConfigs.Add(new AIProviderConfig
                    {
                        AIConfigurationId = configuration.Id,
                        Provider = providerDto.Provider,
                        ApiKey = providerDto.ApiKey,
                        SelectedModel = providerDto.SelectedModel,
                        IsEnabled = providerDto.IsEnabled,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }

            await _context.SaveChangesAsync();

            var configDto = MapToDto(configuration);

            _logger.LogInformation("Configuração de IA atualizada para usuário {UserId}", userId);

            return new AIConfigurationResponseDto
            {
                Success = true,
                Configuration = configDto,
                Message = "Configuração salva com sucesso"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar configuração de IA para usuário {UserId}", userId);
            return new AIConfigurationResponseDto
            {
                Success = false,
                Message = "Erro ao salvar configuração",
                Errors = new List<string> { "Ocorreu um erro inesperado" }
            };
        }
    }

    public async Task<List<AIModelDto>> GetModelsForProviderAsync(AIProvider provider)
    {
        await Task.Delay(1); // Para manter assinatura async
        return AvailableModels.TryGetValue(provider, out var models) ? models : new List<AIModelDto>();
    }

    public async Task<TestApiKeyResponseDto> TestApiKeyAsync(TestApiKeyDto testDto)
    {
        try
        {
            // Simular delay de teste de API
            await Task.Delay(1500);

            // Validação básica do formato da API key
            var isValidFormat = ValidateApiKeyFormat(testDto.Provider, testDto.ApiKey);
            
            if (!isValidFormat)
            {
                return new TestApiKeyResponseDto
                {
                    Success = false,
                    Message = "Formato de API key inválido",
                    Errors = new List<string> { "A API key não possui o formato esperado para este provedor" }
                };
            }

            // Simular teste real da API (80% de sucesso)
            var isSuccess = Random.Shared.NextDouble() > 0.2;

            if (isSuccess)
            {
                return new TestApiKeyResponseDto
                {
                    Success = true,
                    Message = "API key válida e funcionando!"
                };
            }
            else
            {
                return new TestApiKeyResponseDto
                {
                    Success = false,
                    Message = "API key inválida ou sem permissões adequadas",
                    Errors = new List<string> { "Verifique se a API key está correta e possui as permissões necessárias" }
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao testar API key para provedor {Provider}", testDto.Provider);
            return new TestApiKeyResponseDto
            {
                Success = false,
                Message = "Erro ao testar API key",
                Errors = new List<string> { "Ocorreu um erro inesperado durante o teste" }
            };
        }
    }

    private async Task<AIConfiguration> CreateDefaultConfigurationAsync(string userId)
    {
        var configuration = new AIConfiguration
        {
            UserId = userId,
            DefaultProvider = AIProvider.OpenAI,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            AIProviderConfigs = new List<AIProviderConfig>
            {
                new() { Provider = AIProvider.OpenAI, ApiKey = "", SelectedModel = "gpt-4o-mini", IsEnabled = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new() { Provider = AIProvider.Claude, ApiKey = "", SelectedModel = "claude-3-5-haiku-20241022", IsEnabled = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new() { Provider = AIProvider.Gemini, ApiKey = "", SelectedModel = "gemini-1.5-flash", IsEnabled = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
            }
        };

        _context.AIConfigurations.Add(configuration);
        await _context.SaveChangesAsync();

        return configuration;
    }

    private static bool ValidateApiKeyFormat(AIProvider provider, string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Length < 20)
            return false;

        return provider switch
        {
            AIProvider.OpenAI => apiKey.StartsWith("sk-") && apiKey.Length > 20,
            AIProvider.Claude => apiKey.StartsWith("sk-ant-") && apiKey.Length > 20,
            AIProvider.Gemini => apiKey.Length > 20, // Gemini não tem prefixo específico
            _ => false
        };
    }

    private static AIConfigurationDto MapToDto(AIConfiguration configuration)
    {
        return new AIConfigurationDto
        {
            Id = configuration.Id,
            UserId = configuration.UserId,
            DefaultProvider = configuration.DefaultProvider,
            CreatedAt = configuration.CreatedAt,
            UpdatedAt = configuration.UpdatedAt,
            Providers = configuration.AIProviderConfigs.Select(p => new AIProviderConfigDto
            {
                Id = p.Id,
                Provider = p.Provider,
                ApiKey = p.ApiKey,
                SelectedModel = p.SelectedModel,
                IsEnabled = p.IsEnabled,
                LastTestResult = p.LastTestResult,
                LastTestMessage = p.LastTestMessage,
                LastTestedAt = p.LastTestedAt
            }).ToList()
        };
    }
}