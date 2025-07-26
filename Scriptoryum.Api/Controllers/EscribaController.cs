using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Scriptoryum.Api.Application.Dtos;
using Scriptoryum.Api.Application.Services;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace Scriptoryum.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class EscribaController : ControllerBase
{
    private readonly IEscribaService _escribaService;
    private readonly ILogger<EscribaController> _logger;

    public EscribaController(IEscribaService escribaService, ILogger<EscribaController> logger)
    {
        _escribaService = escribaService;
        _logger = logger;
    }

    [HttpGet("sessions")]
    [SwaggerOperation(Summary = "Obter sessões de chat", Description = "Retorna todas as sessões de chat do usuário")]
    [SwaggerResponse(200, "Sessões carregadas com sucesso", typeof(ChatSessionsResponseDto))]
    [SwaggerResponse(401, "Não autorizado")]
    [SwaggerResponse(500, "Erro interno do servidor")]
    public async Task<ActionResult<ChatSessionsResponseDto>> GetChatSessions()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ChatSessionsResponseDto
                {
                    Success = false,
                    Message = "Usuário não autenticado",
                    Errors = new List<string> { "Token de autenticação inválido" }
                });
            }

            var result = await _escribaService.GetChatSessionsAsync(userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter sessões de chat");
            return StatusCode(500, new ChatSessionsResponseDto
            {
                Success = false,
                Message = "Erro interno do servidor",
                Errors = new List<string> { "Ocorreu um erro inesperado" }
            });
        }
    }

    [HttpPost("sessions")]
    [SwaggerOperation(Summary = "Criar nova sessão de chat", Description = "Cria uma nova sessão de chat")]
    [SwaggerResponse(201, "Sessão criada com sucesso", typeof(ChatSessionResponseDto))]
    [SwaggerResponse(400, "Dados inválidos")]
    [SwaggerResponse(401, "Não autorizado")]
    [SwaggerResponse(500, "Erro interno do servidor")]
    public async Task<ActionResult<ChatSessionResponseDto>> CreateChatSession([FromBody] CreateChatSessionDto createDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ChatSessionResponseDto
                {
                    Success = false,
                    Message = "Dados inválidos",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ChatSessionResponseDto
                {
                    Success = false,
                    Message = "Usuário não autenticado",
                    Errors = new List<string> { "Token de autenticação inválido" }
                });
            }

            var result = await _escribaService.CreateChatSessionAsync(userId, createDto);
            
            if (result.Success)
                return CreatedAtAction(nameof(GetChatSession), new { sessionId = result.Session?.Id }, result);
            
            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar sessão de chat");
            return StatusCode(500, new ChatSessionResponseDto
            {
                Success = false,
                Message = "Erro interno do servidor",
                Errors = new List<string> { "Ocorreu um erro inesperado" }
            });
        }
    }

    [HttpGet("sessions/{sessionId}")]
    [SwaggerOperation(Summary = "Obter sessão de chat", Description = "Retorna uma sessão de chat específica com suas mensagens")]
    [SwaggerResponse(200, "Sessão carregada com sucesso", typeof(ChatSessionResponseDto))]
    [SwaggerResponse(401, "Não autorizado")]
    [SwaggerResponse(404, "Sessão não encontrada")]
    [SwaggerResponse(500, "Erro interno do servidor")]
    public async Task<ActionResult<ChatSessionResponseDto>> GetChatSession(int sessionId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ChatSessionResponseDto
                {
                    Success = false,
                    Message = "Usuário não autenticado",
                    Errors = new List<string> { "Token de autenticação inválido" }
                });
            }

            var result = await _escribaService.GetChatSessionAsync(userId, sessionId);
            
            if (!result.Success && result.Message == "Sessão não encontrada")
                return NotFound(result);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter sessão de chat {SessionId}", sessionId);
            return StatusCode(500, new ChatSessionResponseDto
            {
                Success = false,
                Message = "Erro interno do servidor",
                Errors = new List<string> { "Ocorreu um erro inesperado" }
            });
        }
    }

    [HttpDelete("sessions/{sessionId}")]
    [SwaggerOperation(Summary = "Deletar sessão de chat", Description = "Remove uma sessão de chat e todas suas mensagens")]
    [SwaggerResponse(200, "Sessão deletada com sucesso", typeof(ChatSessionResponseDto))]
    [SwaggerResponse(401, "Não autorizado")]
    [SwaggerResponse(404, "Sessão não encontrada")]
    [SwaggerResponse(500, "Erro interno do servidor")]
    public async Task<ActionResult<ChatSessionResponseDto>> DeleteChatSession(int sessionId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ChatSessionResponseDto
                {
                    Success = false,
                    Message = "Usuário não autenticado",
                    Errors = new List<string> { "Token de autenticação inválido" }
                });
            }

            var result = await _escribaService.DeleteChatSessionAsync(userId, sessionId);
            
            if (!result.Success && result.Message == "Sessão não encontrada")
                return NotFound(result);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao deletar sessão de chat {SessionId}", sessionId);
            return StatusCode(500, new ChatSessionResponseDto
            {
                Success = false,
                Message = "Erro interno do servidor",
                Errors = new List<string> { "Ocorreu um erro inesperado" }
            });
        }
    }

    [HttpPatch("sessions/{sessionId}")]
    [SwaggerOperation(Summary = "Atualizar sessão de chat", Description = "Atualiza o título e descrição de uma sessão de chat")]
    [SwaggerResponse(200, "Sessão atualizada com sucesso", typeof(ChatSessionResponseDto))]
    [SwaggerResponse(400, "Dados inválidos")]
    [SwaggerResponse(401, "Não autorizado")]
    [SwaggerResponse(404, "Sessão não encontrada")]
    [SwaggerResponse(500, "Erro interno do servidor")]
    public async Task<ActionResult<ChatSessionResponseDto>> UpdateChatSession(int sessionId, [FromBody] UpdateChatSessionDto updateDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ChatSessionResponseDto
                {
                    Success = false,
                    Message = "Dados inválidos",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ChatSessionResponseDto
                {
                    Success = false,
                    Message = "Usuário não autenticado",
                    Errors = new List<string> { "Token de autenticação inválido" }
                });
            }

            var result = await _escribaService.UpdateChatSessionTitleAsync(userId, sessionId, updateDto);
            
            if (!result.Success && result.Message == "Sessão não encontrada")
                return NotFound(result);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar sessão de chat {SessionId}", sessionId);
            return StatusCode(500, new ChatSessionResponseDto
            {
                Success = false,
                Message = "Erro interno do servidor",
                Errors = new List<string> { "Ocorreu um erro inesperado" }
            });
        }
    }

    [HttpPost("chat")]
    [SwaggerOperation(Summary = "Enviar mensagem", Description = "Envia uma mensagem para o chat e recebe resposta da IA")]
    [SwaggerResponse(200, "Mensagem enviada com sucesso", typeof(SendMessageResponseDto))]
    [SwaggerResponse(400, "Dados inválidos")]
    [SwaggerResponse(401, "Não autorizado")]
    [SwaggerResponse(500, "Erro interno do servidor")]
    public async Task<ActionResult<SendMessageResponseDto>> SendMessage([FromBody] SendMessageDto messageDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new SendMessageResponseDto
                {
                    Success = false,
                    Message = "Dados inválidos",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new SendMessageResponseDto
                {
                    Success = false,
                    Message = "Usuário não autenticado",
                    Errors = new List<string> { "Token de autenticação inválido" }
                });
            }

            var result = await _escribaService.SendMessageAsync(userId, messageDto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar mensagem");
            return StatusCode(500, new SendMessageResponseDto
            {
                Success = false,
                Message = "Erro interno do servidor",
                Errors = new List<string> { "Ocorreu um erro inesperado" }
            });
        }
    }

    [HttpPost("stream")]
    [SwaggerOperation(Summary = "Enviar mensagem com streaming", Description = "Envia uma mensagem e recebe resposta em streaming")]
    [SwaggerResponse(200, "Stream iniciado com sucesso")]
    [SwaggerResponse(400, "Dados inválidos")]
    [SwaggerResponse(401, "Não autorizado")]
    [SwaggerResponse(500, "Erro interno do servidor")]
    public async Task<IActionResult> SendMessageStream([FromBody] SendMessageDto messageDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new SendMessageResponseDto
                {
                    Success = false,
                    Message = "Dados inválidos",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new SendMessageResponseDto
                {
                    Success = false,
                    Message = "Usuário não autenticado",
                    Errors = new List<string> { "Token de autenticação inválido" }
                });
            }

            Response.Headers.Add("Content-Type", "text/event-stream");
            Response.Headers.Add("Cache-Control", "no-cache");
            Response.Headers.Add("Connection", "keep-alive");

            // Simular streaming
            var chunks = new[]
            {
                "Analisando sua pergunta...",
                " Com base no contexto fornecido,",
                " posso identificar alguns pontos importantes.",
                " A resposta completa está sendo processada.",
                " Aqui está a análise detalhada que você solicitou."
            };

            foreach (var chunk in chunks)
            {
                await Response.WriteAsync($"data: {chunk}\n\n");
                await Response.Body.FlushAsync();
                await Task.Delay(500);
            }

            await Response.WriteAsync("data: [DONE]\n\n");
            await Response.Body.FlushAsync();

            return new EmptyResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar mensagem em streaming");
            return StatusCode(500, new SendMessageResponseDto
            {
                Success = false,
                Message = "Erro interno do servidor",
                Errors = new List<string> { "Ocorreu um erro inesperado" }
            });
        }
    }

    [HttpGet("documents/{documentId}/context")]
    [SwaggerOperation(Summary = "Obter contexto do documento", Description = "Retorna o contexto de um documento para uso no chat")]
    [SwaggerResponse(200, "Contexto carregado com sucesso", typeof(DocumentContextResponseDto))]
    [SwaggerResponse(401, "Não autorizado")]
    [SwaggerResponse(404, "Documento não encontrado")]
    [SwaggerResponse(500, "Erro interno do servidor")]
    public async Task<ActionResult<DocumentContextResponseDto>> GetDocumentContext(int documentId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new DocumentContextResponseDto
                {
                    Success = false,
                    Message = "Usuário não autenticado",
                    Errors = new List<string> { "Token de autenticação inválido" }
                });
            }

            var result = await _escribaService.GetDocumentContextAsync(userId, documentId);
            
            if (!result.Success && result.Message == "Documento não encontrado")
                return NotFound(result);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter contexto do documento {DocumentId}", documentId);
            return StatusCode(500, new DocumentContextResponseDto
            {
                Success = false,
                Message = "Erro interno do servidor",
                Errors = new List<string> { "Ocorreu um erro inesperado" }
            });
        }
    }

    [HttpGet("documents/{documentId}/summary")]
    [SwaggerOperation(Summary = "Obter resumo do documento", Description = "Gera um resumo automático do documento")]
    [SwaggerResponse(200, "Resumo gerado com sucesso", typeof(AnalysisResponseDto))]
    [SwaggerResponse(401, "Não autorizado")]
    [SwaggerResponse(404, "Documento não encontrado")]
    [SwaggerResponse(500, "Erro interno do servidor")]
    public async Task<ActionResult<AnalysisResponseDto>> GetDocumentSummary(int documentId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new AnalysisResponseDto
                {
                    Success = false,
                    Message = "Usuário não autenticado",
                    Errors = new List<string> { "Token de autenticação inválido" }
                });
            }

            var result = await _escribaService.GetDocumentSummaryAsync(userId, documentId);
            
            if (!result.Success && result.Message == "Documento não encontrado")
                return NotFound(result);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter resumo do documento {DocumentId}", documentId);
            return StatusCode(500, new AnalysisResponseDto
            {
                Success = false,
                Message = "Erro interno do servidor",
                Errors = new List<string> { "Ocorreu um erro inesperado" }
            });
        }
    }

    [HttpPost("suggestions")]
    [SwaggerOperation(Summary = "Obter sugestões", Description = "Gera sugestões de perguntas baseadas no contexto")]
    [SwaggerResponse(200, "Sugestões geradas com sucesso", typeof(SuggestionsResponseDto))]
    [SwaggerResponse(400, "Dados inválidos")]
    [SwaggerResponse(401, "Não autorizado")]
    [SwaggerResponse(500, "Erro interno do servidor")]
    public async Task<ActionResult<SuggestionsResponseDto>> GetSuggestions([FromBody] GetSuggestionsDto suggestionsDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new SuggestionsResponseDto
                {
                    Success = false,
                    Message = "Dados inválidos",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new SuggestionsResponseDto
                {
                    Success = false,
                    Message = "Usuário não autenticado",
                    Errors = new List<string> { "Token de autenticação inválido" }
                });
            }

            var result = await _escribaService.GetSuggestionsAsync(userId, suggestionsDto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter sugestões");
            return StatusCode(500, new SuggestionsResponseDto
            {
                Success = false,
                Message = "Erro interno do servidor",
                Errors = new List<string> { "Ocorreu um erro inesperado" }
            });
        }
    }

    [HttpPost("analyze")]
    [SwaggerOperation(Summary = "Analisar documento", Description = "Realiza análise específica de um documento")]
    [SwaggerResponse(200, "Análise realizada com sucesso", typeof(AnalysisResponseDto))]
    [SwaggerResponse(400, "Dados inválidos")]
    [SwaggerResponse(401, "Não autorizado")]
    [SwaggerResponse(404, "Documento não encontrado")]
    [SwaggerResponse(500, "Erro interno do servidor")]
    public async Task<ActionResult<AnalysisResponseDto>> AnalyzeDocument([FromBody] AnalyzeDocumentDto analyzeDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new AnalysisResponseDto
                {
                    Success = false,
                    Message = "Dados inválidos",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new AnalysisResponseDto
                {
                    Success = false,
                    Message = "Usuário não autenticado",
                    Errors = new List<string> { "Token de autenticação inválido" }
                });
            }

            var result = await _escribaService.AnalyzeDocumentAsync(userId, analyzeDto);
            
            if (!result.Success && result.Message == "Documento não encontrado")
                return NotFound(result);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao analisar documento");
            return StatusCode(500, new AnalysisResponseDto
            {
                Success = false,
                Message = "Erro interno do servidor",
                Errors = new List<string> { "Ocorreu um erro inesperado" }
            });
        }
    }

    [HttpPost("compare")]
    [SwaggerOperation(Summary = "Comparar documentos", Description = "Compara múltiplos documentos")]
    [SwaggerResponse(200, "Comparação realizada com sucesso", typeof(AnalysisResponseDto))]
    [SwaggerResponse(400, "Dados inválidos")]
    [SwaggerResponse(401, "Não autorizado")]
    [SwaggerResponse(404, "Documentos não encontrados")]
    [SwaggerResponse(500, "Erro interno do servidor")]
    public async Task<ActionResult<AnalysisResponseDto>> CompareDocuments([FromBody] CompareDocumentsDto compareDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new AnalysisResponseDto
                {
                    Success = false,
                    Message = "Dados inválidos",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new AnalysisResponseDto
                {
                    Success = false,
                    Message = "Usuário não autenticado",
                    Errors = new List<string> { "Token de autenticação inválido" }
                });
            }

            var result = await _escribaService.CompareDocumentsAsync(userId, compareDto);
            
            if (!result.Success && result.Message == "Alguns documentos não foram encontrados")
                return NotFound(result);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao comparar documentos");
            return StatusCode(500, new AnalysisResponseDto
            {
                Success = false,
                Message = "Erro interno do servidor",
                Errors = new List<string> { "Ocorreu um erro inesperado" }
            });
        }
    }

    [HttpPost("search")]
    [SwaggerOperation(Summary = "Buscar documentos", Description = "Realiza busca semântica nos documentos")]
    [SwaggerResponse(200, "Busca realizada com sucesso", typeof(SearchResultsResponseDto))]
    [SwaggerResponse(400, "Dados inválidos")]
    [SwaggerResponse(401, "Não autorizado")]
    [SwaggerResponse(500, "Erro interno do servidor")]
    public async Task<ActionResult<SearchResultsResponseDto>> SearchDocuments([FromBody] SearchDocumentsDto searchDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new SearchResultsResponseDto
                {
                    Success = false,
                    Message = "Dados inválidos",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new SearchResultsResponseDto
                {
                    Success = false,
                    Message = "Usuário não autenticado",
                    Errors = new List<string> { "Token de autenticação inválido" }
                });
            }

            var result = await _escribaService.SearchDocumentsAsync(userId, searchDto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar documentos");
            return StatusCode(500, new SearchResultsResponseDto
            {
                Success = false,
                Message = "Erro interno do servidor",
                Errors = new List<string> { "Ocorreu um erro inesperado" }
            });
        }
    }
}