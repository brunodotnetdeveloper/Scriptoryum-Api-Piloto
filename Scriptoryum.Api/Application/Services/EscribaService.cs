using Microsoft.EntityFrameworkCore;
using Scriptoryum.Api.Application.Dtos;
using Scriptoryum.Api.Domain.Entities;
using Scriptoryum.Api.Domain.Enums;
using Scriptoryum.Api.Infrastructure.Context;
using Scriptoryum.Api.Infrastructure.Services;

namespace Scriptoryum.Api.Application.Services;

public interface IEscribaService
{
    Task<ChatSessionsResponseDto> GetChatSessionsAsync(string userId);
    Task<ChatSessionResponseDto> CreateChatSessionAsync(string userId, CreateChatSessionDto createDto);
    Task<ChatSessionResponseDto> GetChatSessionAsync(string userId, int sessionId);
    Task<ChatSessionResponseDto> DeleteChatSessionAsync(string userId, int sessionId);
    Task<ChatSessionResponseDto> UpdateChatSessionTitleAsync(string userId, int sessionId, UpdateChatSessionDto updateDto);
    Task<SendMessageResponseDto> SendMessageAsync(string userId, SendMessageDto messageDto);
    Task<DocumentContextResponseDto> GetDocumentContextAsync(string userId, int documentId);
    Task<AnalysisResponseDto> GetDocumentSummaryAsync(string userId, int documentId);
    Task<SuggestionsResponseDto> GetSuggestionsAsync(string userId, GetSuggestionsDto suggestionsDto);
    Task<AnalysisResponseDto> AnalyzeDocumentAsync(string userId, AnalyzeDocumentDto analyzeDto);
    Task<AnalysisResponseDto> CompareDocumentsAsync(string userId, CompareDocumentsDto compareDto);
    Task<SearchResultsResponseDto> SearchDocumentsAsync(string userId, SearchDocumentsDto searchDto);
}

public class EscribaService : IEscribaService
{
    private readonly ScriptoryumDbContext _context;
    private readonly ILogger<EscribaService> _logger;
    private readonly IAIService _aiService;
    private readonly IRagService _ragService;

    public EscribaService(
        ScriptoryumDbContext context, 
        ILogger<EscribaService> logger,
        IAIService aiService,
        IRagService ragService)
    {
        _context = context;
        _logger = logger;
        _aiService = aiService;
        _ragService = ragService;
    }

    public async Task<ChatSessionsResponseDto> GetChatSessionsAsync(string userId)
    {
        try
        {
            var sessions = await _context.ChatSessions
                .Where(s => s.UserId == userId)
                .Include(s => s.Document)
                .OrderByDescending(s => s.LastActivityAt)
                .Select(s => new ChatSessionDto
                {
                    Id = s.Id,
                    UserId = s.UserId,
                    Title = s.Title,
                    Description = s.Description,
                    DocumentId = s.DocumentId,
                    DocumentName = s.Document != null ? s.Document.OriginalFileName : null,
                    MessageCount = s.MessageCount,
                    LastActivityAt = s.LastActivityAt,
                    CreatedAt = s.CreatedAt,
                    UpdatedAt = s.UpdatedAt
                })
                .ToListAsync();

            return new ChatSessionsResponseDto
            {
                Success = true,
                Sessions = sessions,
                Message = "Sessões carregadas com sucesso"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao carregar sessões de chat para usuário {UserId}", userId);
            return new ChatSessionsResponseDto
            {
                Success = false,
                Message = "Erro ao carregar sessões",
                Errors = new List<string> { "Ocorreu um erro inesperado" }
            };
        }
    }

    public async Task<ChatSessionResponseDto> CreateChatSessionAsync(string userId, CreateChatSessionDto createDto)
    {
        try
        {
            var session = new ChatSession
            {
                UserId = userId,
                Title = createDto.Title ?? "Nova Conversa",
                Description = createDto.Description,
                DocumentId = createDto.DocumentId,
                MessageCount = 0,
                LastActivityAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.ChatSessions.Add(session);
            await _context.SaveChangesAsync();

            // Carregar com dados relacionados
            var sessionWithData = await _context.ChatSessions
                .Include(s => s.Document)
                .FirstAsync(s => s.Id == session.Id);

            var sessionDto = MapToDto(sessionWithData);

            _logger.LogInformation("Nova sessão de chat criada para usuário {UserId}: {SessionId}", userId, session.Id);

            return new ChatSessionResponseDto
            {
                Success = true,
                Session = sessionDto,
                Message = "Sessão criada com sucesso"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar sessão de chat para usuário {UserId}", userId);
            return new ChatSessionResponseDto
            {
                Success = false,
                Message = "Erro ao criar sessão",
                Errors = new List<string> { "Ocorreu um erro inesperado" }
            };
        }
    }

    public async Task<ChatSessionResponseDto> GetChatSessionAsync(string userId, int sessionId)
    {
        try
        {
            var session = await _context.ChatSessions
                .Include(s => s.Document)
                .Include(s => s.Messages.OrderBy(m => m.CreatedAt))
                .ThenInclude(m => m.Document)
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId);

            if (session == null)
            {
                return new ChatSessionResponseDto
                {
                    Success = false,
                    Message = "Sessão não encontrada",
                    Errors = new List<string> { "A sessão especificada não existe ou não pertence ao usuário" }
                };
            }

            var sessionDto = MapToDto(session);

            return new ChatSessionResponseDto
            {
                Success = true,
                Session = sessionDto,
                Message = "Sessão carregada com sucesso"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao carregar sessão {SessionId} para usuário {UserId}", sessionId, userId);
            return new ChatSessionResponseDto
            {
                Success = false,
                Message = "Erro ao carregar sessão",
                Errors = new List<string> { "Ocorreu um erro inesperado" }
            };
        }
    }

    public async Task<ChatSessionResponseDto> DeleteChatSessionAsync(string userId, int sessionId)
    {
        try
        {
            var session = await _context.ChatSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId);

            if (session == null)
            {
                return new ChatSessionResponseDto
                {
                    Success = false,
                    Message = "Sessão não encontrada",
                    Errors = new List<string> { "A sessão especificada não existe ou não pertence ao usuário" }
                };
            }

            _context.ChatSessions.Remove(session);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Sessão {SessionId} deletada para usuário {UserId}", sessionId, userId);

            return new ChatSessionResponseDto
            {
                Success = true,
                Message = "Sessão deletada com sucesso"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao deletar sessão {SessionId} para usuário {UserId}", sessionId, userId);
            return new ChatSessionResponseDto
            {
                Success = false,
                Message = "Erro ao deletar sessão",
                Errors = new List<string> { "Ocorreu um erro inesperado" }
            };
        }
    }

    public async Task<ChatSessionResponseDto> UpdateChatSessionTitleAsync(string userId, int sessionId, UpdateChatSessionDto updateDto)
    {
        try
        {
            var session = await _context.ChatSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId);

            if (session == null)
            {
                return new ChatSessionResponseDto
                {
                    Success = false,
                    Message = "Sessão não encontrada",
                    Errors = new List<string> { "A sessão especificada não existe ou não pertence ao usuário" }
                };
            }

            session.Title = updateDto.Title;
            session.Description = updateDto.Description;
            session.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var sessionDto = MapToDto(session);

            _logger.LogInformation("Título da sessão {SessionId} atualizado para usuário {UserId}", sessionId, userId);

            return new ChatSessionResponseDto
            {
                Success = true,
                Session = sessionDto,
                Message = "Sessão atualizada com sucesso"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar sessão {SessionId} para usuário {UserId}", sessionId, userId);
            return new ChatSessionResponseDto
            {
                Success = false,
                Message = "Erro ao atualizar sessão",
                Errors = new List<string> { "Ocorreu um erro inesperado" }
            };
        }
    }

    public async Task<SendMessageResponseDto> SendMessageAsync(string userId, SendMessageDto messageDto)
    {
        try
        {
            ChatSession session;

            // Se não há sessionId, criar nova sessão
            if (!messageDto.SessionId.HasValue)
            {
                session = new ChatSession
                {
                    UserId = userId,
                    Title = "Nova Conversa",
                    DocumentId = messageDto.DocumentId,
                    MessageCount = 0,
                    LastActivityAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.ChatSessions.Add(session);
                await _context.SaveChangesAsync();
            }
            else
            {
                session = await _context.ChatSessions
                    .FirstOrDefaultAsync(s => s.Id == messageDto.SessionId.Value && s.UserId == userId);

                if (session == null)
                {
                    return new SendMessageResponseDto
                    {
                        Success = false,
                        Message = "Sessão não encontrada",
                        Errors = new List<string> { "A sessão especificada não existe ou não pertence ao usuário" }
                    };
                }
            }

            // Criar mensagem do usuário
            var userMessage = new ChatMessage
            {
                ChatSessionId = session.Id,
                Role = MessageRole.User,
                Content = messageDto.Message,
                DocumentId = messageDto.DocumentId,
                DocumentName = messageDto.DocumentId.HasValue ? await GetDocumentNameAsync(messageDto.DocumentId.Value) : null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.ChatMessages.Add(userMessage);

            // Simular resposta do assistente
            var assistantResponse = await GenerateAssistantResponseAsync(messageDto.Message, messageDto.Context, userId, messageDto.DocumentId);
            
            var assistantMessage = new ChatMessage
            {
                ChatSessionId = session.Id,
                Role = MessageRole.Assistant,
                Content = assistantResponse.Response,
                DocumentId = messageDto.DocumentId,
                DocumentName = userMessage.DocumentName,
                TokenCount = assistantResponse.TokenCount,
                Cost = assistantResponse.Cost,
                AIProvider = assistantResponse.AIProvider,
                ModelUsed = assistantResponse.ModelUsed,
                ResponseTimeMs = assistantResponse.ResponseTimeMs,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.ChatMessages.Add(assistantMessage);

            // Atualizar sessão
            session.MessageCount += 2;
            session.LastActivityAt = DateTime.UtcNow;
            session.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Mensagem enviada na sessão {SessionId} para usuário {UserId}", session.Id, userId);

            return new SendMessageResponseDto
            {
                Success = true,
                SessionId = session.Id,
                MessageId = assistantMessage.Id,
                Response = assistantResponse.Response,
                Suggestions = assistantResponse.Suggestions,
                Message = "Mensagem enviada com sucesso"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar mensagem para usuário {UserId}", userId);
            return new SendMessageResponseDto
            {
                Success = false,
                Message = "Erro ao enviar mensagem",
                Errors = new List<string> { "Ocorreu um erro inesperado" }
            };
        }
    }

    public async Task<DocumentContextResponseDto> GetDocumentContextAsync(string userId, int documentId)
    {
        try
        {
            var document = await _context.Documents
                .Include(d => d.ExtractedEntities)
                .Include(d => d.Insights)
                .Include(d => d.RisksDetected)
                .FirstOrDefaultAsync(d => d.Id == documentId && d.UploadedByUserId == userId);

            if (document == null)
            {
                return new DocumentContextResponseDto
                {
                    Success = false,
                    Message = "Documento não encontrado",
                    Errors = new List<string> { "O documento especificado não existe ou não pertence ao usuário" }
                };
            }

            var contextDto = new DocumentContextDto
            {
                Id = document.Id,
                Name = document.OriginalFileName,
                Content = document.TextExtracted ?? "",
                Entities = document.ExtractedEntities?.Cast<object>().ToList(),
                Insights = document.Insights?.Cast<object>().ToList(),
                Risks = document.RisksDetected?.Cast<object>().ToList()
            };

            return new DocumentContextResponseDto
            {
                Success = true,
                Context = contextDto,
                Message = "Contexto do documento carregado com sucesso"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao carregar contexto do documento {DocumentId} para usuário {UserId}", documentId, userId);
            return new DocumentContextResponseDto
            {
                Success = false,
                Message = "Erro ao carregar contexto do documento",
                Errors = new List<string> { "Ocorreu um erro inesperado" }
            };
        }
    }

    public async Task<AnalysisResponseDto> GetDocumentSummaryAsync(string userId, int documentId)
    {
        try
        {
            var document = await _context.Documents
                .FirstOrDefaultAsync(d => d.Id == documentId && d.UploadedByUserId == userId);

            if (document == null)
            {
                return new AnalysisResponseDto
                {
                    Success = false,
                    Message = "Documento não encontrado",
                    Errors = new List<string> { "O documento especificado não existe ou não pertence ao usuário" }
                };
            }

            // Simular geração de resumo
            await Task.Delay(1000);
            var summary = $"Resumo do documento '{document.OriginalFileName}': Este documento contém informações importantes que foram processadas e analisadas pelo sistema.";

            return new AnalysisResponseDto
            {
                Success = true,
                Analysis = summary,
                Message = "Resumo gerado com sucesso"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar resumo do documento {DocumentId} para usuário {UserId}", documentId, userId);
            return new AnalysisResponseDto
            {
                Success = false,
                Message = "Erro ao gerar resumo",
                Errors = new List<string> { "Ocorreu um erro inesperado" }
            };
        }
    }

    public async Task<SuggestionsResponseDto> GetSuggestionsAsync(string userId, GetSuggestionsDto suggestionsDto)
    {
        try
        {
            // Simular geração de sugestões
            await Task.Delay(500);
            
            var suggestions = new List<string>
            {
                "Analise os principais pontos deste documento",
                "Quais são os riscos identificados?",
                "Resuma as informações mais importantes",
                "Identifique as entidades mencionadas",
                "Compare com documentos similares"
            };

            return new SuggestionsResponseDto
            {
                Success = true,
                Suggestions = suggestions,
                Message = "Sugestões geradas com sucesso"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar sugestões para usuário {UserId}", userId);
            return new SuggestionsResponseDto
            {
                Success = false,
                Message = "Erro ao gerar sugestões",
                Errors = new List<string> { "Ocorreu um erro inesperado" }
            };
        }
    }

    public async Task<AnalysisResponseDto> AnalyzeDocumentAsync(string userId, AnalyzeDocumentDto analyzeDto)
    {
        try
        {
            var document = await _context.Documents
                .FirstOrDefaultAsync(d => d.Id == analyzeDto.DocumentId && d.UploadedByUserId == userId);

            if (document == null)
            {
                return new AnalysisResponseDto
                {
                    Success = false,
                    Message = "Documento não encontrado",
                    Errors = new List<string> { "O documento especificado não existe ou não pertence ao usuário" }
                };
            }

            // Simular análise
            await Task.Delay(2000);
            var analysis = $"Análise do tipo '{analyzeDto.AnalysisType}' para o documento '{document.OriginalFileName}': Análise detalhada realizada com sucesso.";

            return new AnalysisResponseDto
            {
                Success = true,
                Analysis = analysis,
                Message = "Análise realizada com sucesso"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao analisar documento {DocumentId} para usuário {UserId}", analyzeDto.DocumentId, userId);
            return new AnalysisResponseDto
            {
                Success = false,
                Message = "Erro ao analisar documento",
                Errors = new List<string> { "Ocorreu um erro inesperado" }
            };
        }
    }

    public async Task<AnalysisResponseDto> CompareDocumentsAsync(string userId, CompareDocumentsDto compareDto)
    {
        try
        {
            var documents = await _context.Documents
                .Where(d => compareDto.DocumentIds.Contains(d.Id) && d.UploadedByUserId == userId)
                .ToListAsync();

            if (documents.Count != compareDto.DocumentIds.Count)
            {
                return new AnalysisResponseDto
                {
                    Success = false,
                    Message = "Alguns documentos não foram encontrados",
                    Errors = new List<string> { "Nem todos os documentos especificados existem ou pertencem ao usuário" }
                };
            }

            // Simular comparação
            await Task.Delay(3000);
            var comparison = $"Comparação entre {documents.Count} documentos realizada com sucesso. Principais diferenças e semelhanças identificadas.";

            return new AnalysisResponseDto
            {
                Success = true,
                Analysis = comparison,
                Message = "Comparação realizada com sucesso"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao comparar documentos para usuário {UserId}", userId);
            return new AnalysisResponseDto
            {
                Success = false,
                Message = "Erro ao comparar documentos",
                Errors = new List<string> { "Ocorreu um erro inesperado" }
            };
        }
    }

    public async Task<SearchResultsResponseDto> SearchDocumentsAsync(string userId, SearchDocumentsDto searchDto)
    {
        try
        {
            // Simular busca semântica
            await Task.Delay(1500);
            
            var results = new List<object>
            {
                new { DocumentId = 1, Title = "Documento 1", Relevance = 0.95, Snippet = "Trecho relevante encontrado..." },
                new { DocumentId = 2, Title = "Documento 2", Relevance = 0.87, Snippet = "Outro trecho relevante..." }
            };

            return new SearchResultsResponseDto
            {
                Success = true,
                Results = results,
                Message = "Busca realizada com sucesso"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar documentos para usuário {UserId}", userId);
            return new SearchResultsResponseDto
            {
                Success = false,
                Message = "Erro ao buscar documentos",
                Errors = new List<string> { "Ocorreu um erro inesperado" }
            };
        }
    }

    private async Task<string?> GetDocumentNameAsync(int documentId)
    {
        var document = await _context.Documents
            .Where(d => d.Id == documentId)
            .Select(d => d.OriginalFileName)
            .FirstOrDefaultAsync();
        
        return document;
    }

    private async Task<(string Response, List<string> Suggestions, int? TokenCount, decimal? Cost, AIProvider? AIProvider, string? ModelUsed, int? ResponseTimeMs)> GenerateAssistantResponseAsync(string message, string? context, string userId, int? documentId = null)
    {
        try
        {
            _logger.LogInformation("Gerando resposta do assistente para usuário {UserId}", userId);

            // Obter configuração de IA do usuário
            var aiConfig = await _context.AIConfigurations
                .Include(c => c.AIProviderConfigs)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (aiConfig == null)
            {
                _logger.LogWarning("Configuração de IA não encontrada para usuário {UserId}", userId);
                return (
                    Response: "Desculpe, não foi possível processar sua solicitação. Configure sua API key nas configurações.",
                    Suggestions: null,
                    TokenCount: 0,
                    Cost: 0m,
                    AIProvider: null,
                    ModelUsed: null,
                    ResponseTimeMs: 0
                );
            }

            // Obter configuração do provedor ativo
            var activeProviderConfig = aiConfig.AIProviderConfigs
                .FirstOrDefault(p => p.IsEnabled);

            if (activeProviderConfig == null || string.IsNullOrEmpty(activeProviderConfig.ApiKey))
            {
                _logger.LogWarning("Configuração de IA ativa não encontrada ou inválida para usuário {UserId}", userId);
                return (
                    Response: "Desculpe, não foi possível processar sua solicitação. Configure sua API key nas configurações.",
                    Suggestions: null,
                    TokenCount: 0,
                    Cost: 0m,
                    AIProvider: null,
                    ModelUsed: null,
                    ResponseTimeMs: 0
                );
            }

            // Determinar o provedor baseado na configuração
            if (!Enum.TryParse<AIProvider>(activeProviderConfig.Provider, out var aiProvider))
            {
                _logger.LogWarning("Provedor de IA inválido: {Provider} para usuário {UserId}", activeProviderConfig.Provider, userId);
                return (
                    Response: "Desculpe, provedor de IA configurado é inválido. Verifique suas configurações.",
                    Suggestions: null,
                    TokenCount: 0,
                    Cost: 0m,
                    AIProvider: null,
                    ModelUsed: null,
                    ResponseTimeMs: 0
                );
            }

            // Obter contexto relevante usando RAG
            var ragContext = await _ragService.GetRelevantContextAsync(message, userId, documentId, 5);
            
            // Combinar contexto fornecido com contexto RAG
            var finalContext = context;
            if (!string.IsNullOrEmpty(ragContext.Context))
            {
                finalContext = string.IsNullOrEmpty(context) 
                    ? ragContext.Context 
                    : $"{context}\n\n--- Contexto Adicional ---\n{ragContext.Context}";
            }

            // Preparar requisição para o provedor de IA
            var aiRequest = new AIRequest
            {
                Message = message,
                Context = finalContext,
                Model = activeProviderConfig.SelectedModel ?? GetDefaultModel(aiProvider),
                Temperature = float.TryParse(aiConfig.Temperature, out var temp) ? temp : 0.7f,
                MaxTokens = int.TryParse(aiConfig.MaxTokens, out var maxTokens) ? maxTokens : 4000,
                ApiKey = activeProviderConfig.ApiKey,
                Provider = aiProvider
            };

            // Chamar o serviço de IA
            var aiResponse = await _aiService.GenerateResponseAsync(aiRequest);

            if (!aiResponse.Success)
            {
                _logger.LogError("Erro na resposta da API {Provider}: {Error}", aiProvider, aiResponse.ErrorMessage);
                return (
                    Response: "Desculpe, ocorreu um erro ao processar sua solicitação. Tente novamente.",
                    Suggestions: null,
                    TokenCount: 0,
                    Cost: 0m,
                    AIProvider: aiProvider,
                    ModelUsed: aiRequest.Model,
                    ResponseTimeMs: aiResponse.ResponseTimeMs
                );
            }

            // Gerar sugestões baseadas no contexto
            var suggestions = GenerateSuggestions(message, ragContext.RelevantChunks);

            _logger.LogInformation("Resposta {Provider} recebida - Tokens: {Tokens}, Custo: ${Cost}, Tempo: {Time}ms", 
                aiProvider, aiResponse.TokensUsed, aiResponse.Cost, aiResponse.ResponseTimeMs);

            return (
                Response: aiResponse.Response,
                Suggestions: suggestions,
                TokenCount: aiResponse.TokensUsed,
                Cost: aiResponse.Cost,
                AIProvider: aiProvider,
                ModelUsed: aiResponse.Model,
                ResponseTimeMs: aiResponse.ResponseTimeMs
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar resposta do assistente");
            return (
                Response: "Desculpe, ocorreu um erro interno. Tente novamente.",
                Suggestions: null,
                TokenCount: 0,
                Cost: 0m,
                AIProvider: null,
                ModelUsed: null,
                ResponseTimeMs: 0
            );
        }
    }

    private static string GetDefaultModel(AIProvider provider)
    {
        return provider switch
        {
            AIProvider.OpenAI => "gpt-4o-mini",
            AIProvider.Claude => "claude-3-haiku-20240307",
            AIProvider.Gemini => "gemini-1.5-flash",
            _ => "gpt-4o-mini"
        };
    }

    private static List<string> GenerateSuggestions(string message, List<DocumentChunkResult> relevantChunks)
    {
        var suggestions = new List<string>();

        // Sugestões baseadas no contexto dos chunks
        if (relevantChunks.Any())
        {
            var documentNames = relevantChunks.Select(c => c.DocumentName).Distinct().ToList();
            
            if (documentNames.Count == 1)
            {
                suggestions.Add($"Me conte mais sobre {documentNames.First()}");
                suggestions.Add("Quais são os pontos principais deste documento?");
            }
            else if (documentNames.Count > 1)
            {
                suggestions.Add("Compare as informações entre estes documentos");
                suggestions.Add("Quais são as diferenças principais?");
            }
        }

        // Sugestões gerais baseadas no tipo de pergunta
        if (message.ToLower().Contains("resumo") || message.ToLower().Contains("resumir"))
        {
            suggestions.Add("Pode detalhar os pontos mais importantes?");
            suggestions.Add("Quais são as conclusões principais?");
        }
        else if (message.ToLower().Contains("análise") || message.ToLower().Contains("analisar"))
        {
            suggestions.Add("Quais são os riscos identificados?");
            suggestions.Add("Há recomendações específicas?");
        }
        else
        {
            suggestions.Add("Pode me dar mais detalhes sobre este tópico?");
            suggestions.Add("Como isso se relaciona com outros documentos?");
            suggestions.Add("Quais são as implicações práticas?");
        }

        return suggestions.Take(3).ToList();
    }

    private static ChatSessionDto MapToDto(ChatSession session)
    {
        return new ChatSessionDto
        {
            Id = session.Id,
            UserId = session.UserId,
            Title = session.Title,
            Description = session.Description,
            DocumentId = session.DocumentId,
            DocumentName = session.Document?.OriginalFileName,
            MessageCount = session.MessageCount,
            LastActivityAt = session.LastActivityAt,
            CreatedAt = session.CreatedAt,
            UpdatedAt = session.UpdatedAt,
            Messages = session.Messages?.Select(m => new ChatMessageDto
            {
                Id = m.Id,
                ChatSessionId = m.ChatSessionId,
                Role = m.Role,
                Content = m.Content,
                DocumentId = m.DocumentId,
                DocumentName = m.DocumentName,
                TokenCount = m.TokenCount,
                Cost = m.Cost,
                AIProvider = m.AIProvider,
                ModelUsed = m.ModelUsed,
                ResponseTimeMs = m.ResponseTimeMs,
                CreatedAt = m.CreatedAt
            }).ToList() ?? new List<ChatMessageDto>()
        };
    }
}