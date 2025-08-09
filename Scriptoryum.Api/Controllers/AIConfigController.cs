using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Scriptoryum.Api.Application.Dtos;
using Scriptoryum.Api.Application.Services;
using Scriptoryum.Api.Domain.Enums;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace Scriptoryum.Api.Controllers;

[ApiController]
[Route("api/ai-config")]
[Authorize]
[Produces("application/json")]
public class AIConfigController : ControllerBase
{
    private readonly IAIConfigService _aiConfigService;
    private readonly ILogger<AIConfigController> _logger;

    public AIConfigController(IAIConfigService aiConfigService, ILogger<AIConfigController> logger)
    {
        _aiConfigService = aiConfigService;
        _logger = logger;
    }

    /// <summary>
    /// Obtém a configuração de IA do usuário
    /// </summary>
    /// <returns>Configuração de IA do usuário</returns>
    [HttpGet]
    [SwaggerOperation(
        Summary = "Obter configuração de IA",
        Description = "Retorna a configuração de IA do usuário autenticado, incluindo provedores configurados e modelos selecionados"
    )]
    [SwaggerResponse(200, "Configuração carregada com sucesso", typeof(AIConfigurationResponseDto))]
    [SwaggerResponse(401, "Não autorizado")]
    [SwaggerResponse(500, "Erro interno do servidor", typeof(AIConfigurationResponseDto))]
    public async Task<ActionResult<AIConfigurationResponseDto>> GetConfiguration()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new AIConfigurationResponseDto
                {
                    Success = false,
                    Message = "Usuário não autenticado",
                    Errors = new List<string> { "Token inválido ou expirado" }
                });
            }

            var result = await _aiConfigService.GetConfigurationAsync(userId);
            
            if (!result.Success)
            {
                return StatusCode(500, result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter configuração de IA");
            return StatusCode(500, new AIConfigurationResponseDto
            {
                Success = false,
                Message = "Erro interno do servidor",
                Errors = new List<string> { "Ocorreu um erro inesperado" }
            });
        }
    }

    /// <summary>
    /// Atualiza a configuração de IA do usuário
    /// </summary>
    /// <param name="updateDto">Dados da configuração a ser atualizada</param>
    /// <returns>Configuração atualizada</returns>
    [HttpPut]
    [SwaggerOperation(
        Summary = "Atualizar configuração de IA",
        Description = "Atualiza a configuração de IA do usuário, incluindo provedor padrão e configurações específicas de cada provedor"
    )]
    [SwaggerResponse(200, "Configuração atualizada com sucesso", typeof(AIConfigurationResponseDto))]
    [SwaggerResponse(400, "Dados inválidos", typeof(AIConfigurationResponseDto))]
    [SwaggerResponse(401, "Não autorizado")]
    [SwaggerResponse(500, "Erro interno do servidor", typeof(AIConfigurationResponseDto))]
    public async Task<ActionResult<AIConfigurationResponseDto>> UpdateConfiguration([FromBody] UpdateAIConfigurationDto updateDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(new AIConfigurationResponseDto
                {
                    Success = false,
                    Message = "Dados inválidos",
                    Errors = errors
                });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new AIConfigurationResponseDto
                {
                    Success = false,
                    Message = "Usuário não autenticado",
                    Errors = new List<string> { "Token inválido ou expirado" }
                });
            }

            var result = await _aiConfigService.UpdateConfigurationAsync(userId, updateDto);
            
            if (!result.Success)
            {
                return BadRequest(result);
            }

            _logger.LogInformation("Configuração de IA atualizada para usuário {UserId}", userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar configuração de IA");
            return StatusCode(500, new AIConfigurationResponseDto
            {
                Success = false,
                Message = "Erro interno do servidor",
                Errors = new List<string> { "Ocorreu um erro inesperado" }
            });
        }
    }

    /// <summary>
    /// Obtém os modelos disponíveis para um provedor específico
    /// </summary>
    /// <param name="provider">Provedor de IA (openai, claude, gemini)</param>
    /// <returns>Lista de modelos disponíveis</returns>
    [HttpGet("models/{provider}")]
    [SwaggerOperation(
        Summary = "Obter modelos por provedor",
        Description = "Retorna a lista de modelos disponíveis para um provedor específico de IA"
    )]
    [SwaggerResponse(200, "Modelos carregados com sucesso", typeof(List<AIModelDto>))]
    [SwaggerResponse(400, "Provedor inválido")]
    [SwaggerResponse(401, "Não autorizado")]
    [SwaggerResponse(500, "Erro interno do servidor")]
    public async Task<ActionResult<List<AIModelDto>>> GetModelsForProvider([FromRoute] string provider)
    {
        try
        {
            if (!Enum.TryParse<AIProvider>(provider, true, out var aiProvider))
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = "Provedor inválido",
                    Errors = new List<string> { $"Provedor '{provider}' não é válido. Valores aceitos: openai, claude, gemini" }
                });
            }

            var models = await _aiConfigService.GetModelsForProviderAsync(aiProvider);
            return Ok(models);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter modelos para provedor {Provider}", provider);
            return StatusCode(500, new
            {
                Success = false,
                Message = "Erro interno do servidor",
                Errors = new List<string> { "Ocorreu um erro inesperado" }
            });
        }
    }
}