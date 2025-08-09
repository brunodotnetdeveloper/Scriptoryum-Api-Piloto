using Microsoft.EntityFrameworkCore;
using Scriptoryum.Api.Infrastructure.Context;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace Scriptoryum.Api.Infrastructure.Services;

public class RagService : IRagService
{
    private readonly ScriptoryumDbContext _context;
    private readonly IOpenAIService _openAIService;
    private readonly ILogger<RagService> _logger;

    public RagService(
        ScriptoryumDbContext context, 
        IOpenAIService openAIService, 
        ILogger<RagService> logger)
    {
        _context = context;
        _openAIService = openAIService;
        _logger = logger;
    }

    public async Task<RagContext> GetRelevantContextAsync(string query, string userId, int? documentId = null, int maxChunks = 5)
    {
        try
        {
            _logger.LogInformation("Buscando contexto relevante para query: {Query}, UserId: {UserId}, DocumentId: {DocumentId}", 
                query, userId, documentId);

            // Buscar chunks similares
            var similarChunks = await SearchSimilarChunksAsync(query, userId, documentId, maxChunks);

            if (!similarChunks.Any())
            {
                _logger.LogWarning("Nenhum chunk relevante encontrado para a query");
                return new RagContext();
            }

            // Construir contexto combinando os chunks mais relevantes
            var contextBuilder = new List<string>();
            
            foreach (var chunk in similarChunks.Take(maxChunks))
            {
                contextBuilder.Add($"[Documento: {chunk.DocumentName}]\n{chunk.Content}\n");
            }

            var context = string.Join("\n---\n", contextBuilder);
            var averageScore = similarChunks.Average(c => c.SimilarityScore);

            _logger.LogInformation("Contexto RAG construído com {ChunkCount} chunks, score médio: {AverageScore}", 
                similarChunks.Count, averageScore);

            return new RagContext
            {
                Context = context,
                RelevantChunks = similarChunks,
                TotalChunks = similarChunks.Count,
                AverageScore = averageScore
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter contexto relevante");
            return new RagContext();
        }
    }

    public async Task<List<DocumentChunkResult>> SearchSimilarChunksAsync(string query, string userId, int? documentId = null, int maxResults = 10)
    {
        try
        {
            // Buscar configuração de IA do usuário
            var userAIConfig = await _context.AIConfigurations
                .Include(c => c.AIProviderConfigs)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (userAIConfig == null)
            {
                _logger.LogWarning("AIConfiguration não encontrada para o usuário {UserId}", userId);
                return new List<DocumentChunkResult>();
            }

            // Buscar a configuração do provedor padrão
            var defaultProviderConfig = userAIConfig.AIProviderConfigs
                .FirstOrDefault(p => p.Provider == userAIConfig.DefaultProvider && p.IsEnabled);

            if (defaultProviderConfig == null || string.IsNullOrEmpty(defaultProviderConfig.ApiKey))
            {
                _logger.LogWarning("Configuração do provedor padrão não encontrada ou API key não configurada para o usuário {UserId}", userId);
                return new List<DocumentChunkResult>();
            }

            var apiKey = defaultProviderConfig.ApiKey;

            // Gerar embedding da query usando o token do modelo selecionado pelo usuário
            var queryEmbedding = await _openAIService.GenerateEmbeddingAsync(query, apiKey);

            if (queryEmbedding.Length == 0)
            {
                _logger.LogWarning("Não foi possível gerar embedding para a query");
                return new List<DocumentChunkResult>();
            }

            var queryVector = new Vector(queryEmbedding);

            // Construir query base
            var chunksQuery = _context.DocumentChunks
                .Include(dc => dc.Document)
                .Where(dc => dc.Document.UploadedByUserId == userId && dc.Embedding != null);

            // Filtrar por documento específico se fornecido
            if (documentId.HasValue)
            {
                chunksQuery = chunksQuery.Where(dc => dc.DocumentId == documentId.Value);
            }

            // Buscar chunks similares usando cosine similarity
            var results = await chunksQuery
                .Select(dc => new
                {
                    ChunkId = dc.Id,
                    DocumentId = dc.DocumentId,
                    DocumentName = dc.Document.OriginalFileName,
                    Content = dc.Content,
                    ChunkIndex = dc.ChunkIndex,
                    Embedding = dc.Embedding,
                    // Calcular distância coseno usando o método CosineDistance do pgvector
                    Distance = dc.Embedding!.CosineDistance(queryVector)
                })
                .OrderBy(x => x.Distance) // Menor distância = maior similaridade
                .Take(maxResults)
                .ToListAsync();

            var documentChunkResults = results.Select(r => new DocumentChunkResult
            {
                ChunkId = r.ChunkId,
                DocumentId = r.DocumentId,
                DocumentName = r.DocumentName,
                Content = r.Content,
                ChunkIndex = r.ChunkIndex,
                SimilarityScore = 1.0 - r.Distance // Converter distância em score de similaridade
            }).ToList();

            _logger.LogInformation("Encontrados {ResultCount} chunks similares para a query", documentChunkResults.Count);

            return documentChunkResults;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar chunks similares");
            return new List<DocumentChunkResult>();
        }
    }
}