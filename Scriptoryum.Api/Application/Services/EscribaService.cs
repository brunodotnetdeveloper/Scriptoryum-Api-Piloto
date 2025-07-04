using Scriptoryum.Api.Application.Dtos;
using Scriptoryum.Api.Application.Helpers;
using Scriptoryum.Api.Application.Models;
using Scriptoryum.Api.Application.Utils;
using Scriptoryum.Api.Domain.Enums;
using Scriptoryum.Api.Infrastructure.Clients.Gemini;
using Scriptoryum.Api.Infrastructure.Context;
using System.Text.Json;

namespace Scriptoryum.Api.Application.Services;

public interface IEscribaService
{
    Task QueueDocumentAnalysis(int documentId, string userId);
    Task ProcessDocumentAnalysisAsync(DocumentAnalysisMessage message);
    Task<DocumentAnalysisDto> AnalyzeDocument(int documentId);
    Task<List<ExtractedEntityDto>> ExtractEntitiesAsync(int documentId);
    Task<List<RiskDetectedDto>> DetectRisksAsync(int documentId);
    Task<List<InsightDto>> GenerateInsightsAsync(int documentId);
    Task<List<TimelineEventDto>> ExtractTimelineEventsAsync(int documentId);
}

public class EscribaService : IEscribaService
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new RiskLevelJsonConverter(), new EntityTypeJsonConverter(), new InsightCategoryJsonConverter(), new ImportanceLevelJsonConverter() }
    };

    private readonly IBackgroundTaskQueue<DocumentAnalysisMessage> _analysisQueue;
    private readonly ILogger<EscribaService> _logger;
    private readonly GeminiClient geminiClient;
    private readonly ScriptoryumDbContext dbContext;

    public EscribaService(
        GeminiClient geminiClient,
        ScriptoryumDbContext dbContext,
        IBackgroundTaskQueue<DocumentAnalysisMessage> analysisQueue,
        ILogger<EscribaService> logger)
    {
        this.geminiClient = geminiClient;
        this.dbContext = dbContext;
        _analysisQueue = analysisQueue;
        _logger = logger;
    }

    public async Task QueueDocumentAnalysis(int documentId, string userId)
    {
        var document = await dbContext.Documents.FindAsync(documentId) 
            ?? throw new KeyNotFoundException($"Documento com ID {documentId} não encontrado.");

        // Update status to queued
        document.Status = DocumentStatus.Queued;
        await dbContext.SaveChangesAsync();

        // Queue the analysis
        var message = new DocumentAnalysisMessage(documentId, userId, DateTime.UtcNow);
        await _analysisQueue.QueueBackgroundWorkItemAsync(message);
    }

    public async Task ProcessDocumentAnalysisAsync(DocumentAnalysisMessage message)
    {
        var document = await dbContext.Documents.FindAsync(message.DocumentId);
        if (document == null) return;

        try
        {
            document.Status = DocumentStatus.AnalyzingContent;
            await dbContext.SaveChangesAsync();

            var analysis = new DocumentAnalysisDto
            {
                DocumentId = message.DocumentId,
                TextExtracted = document.TextExtracted
            };

            var hasErrors = false;

            // Process each analysis step independently
            try
            {
                analysis.ExtractedEntities = await ExtractEntitiesAsync(message.DocumentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting entities for document {DocumentId}", message.DocumentId);
                document.Status = DocumentStatus.EntitiesExtractionFailed;
                hasErrors = true;
            }

            try
            {
                analysis.DetectedRisks = await DetectRisksAsync(message.DocumentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting risks for document {DocumentId}", message.DocumentId);
                document.Status = DocumentStatus.RisksAnalysisFailed;
                hasErrors = true;
            }

            try
            {
                analysis.GeneratedInsights = await GenerateInsightsAsync(message.DocumentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating insights for document {DocumentId}", message.DocumentId);
                document.Status = DocumentStatus.InsightsGenerationFailed;
                hasErrors = true;
            }

            // Set final status based on overall result
            document.Status = hasErrors ? DocumentStatus.PartiallyProcessed : DocumentStatus.Processed;
            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error processing document {DocumentId}", message.DocumentId);
            document.Status = DocumentStatus.Failed;
            await dbContext.SaveChangesAsync();
            throw;
        }
    }

    public async Task<DocumentAnalysisDto> AnalyzeDocument(int documentId)
    {
        var document = await dbContext.Documents.FindAsync(documentId) ?? throw new KeyNotFoundException($"Documento com ID {documentId} não encontrado.");

        var analysis = new DocumentAnalysisDto
        {
            DocumentId = documentId,
            TextExtracted = document.TextExtracted,
            ExtractedEntities = await ExtractEntitiesAsync(documentId),
            DetectedRisks = await DetectRisksAsync(documentId),
            GeneratedInsights = await GenerateInsightsAsync(documentId)
        };

        return analysis;
    }

    public async Task<List<RiskDetectedDto>> DetectRisksAsync(int documentId)
    {
        var document = await dbContext.Documents.FindAsync(documentId) ?? throw new KeyNotFoundException($"Documento com ID {documentId} não encontrado.");

        var systemPrompt = "Você é um assistente jurídico especializado em análise de documentos. Identifique todos os riscos presentes no texto fornecido. Para cada risco, forneça: descrição, nível de risco (Low, Medium, High, Critical), trecho do texto que evidencia o risco e um score de confiança (0-1). Responda em JSON com a estrutura: [{ \"Description\": \"...\", \"RiskLevel\": \"...\", \"ConfidenceScore\": 0.95, \"EvidenceExcerpt\": \"...\" }]";

        var chunks = TextChunker.ChunkText(document.TextExtracted, 12000);
        var allRisks = new List<RiskDetectedDto>();
        var now = DateTime.UtcNow;

        using var transaction = await dbContext.Database.BeginTransactionAsync();

        try
        {
            foreach (var chunk in chunks)
            {
                var response = await geminiClient.SendMessageAsync(chunk, systemPrompt, "gemini-1.5-flash");

                List<RiskDetectedDto> risksChunk;
                try
                {
                    risksChunk = JsonSerializer.Deserialize<List<RiskDetectedDto>>(response, _jsonOptions);
                }
                catch
                {
                    continue;
                }

                if (risksChunk?.Any() != true) continue;

                foreach (var risk in risksChunk)
                {
                    var entity = new Domain.Entities.RiskDetected
                    {
                        Description = risk.Description,
                        RiskLevel = risk.RiskLevel,
                        ConfidenceScore = risk.ConfidenceScore,
                        EvidenceExcerpt = risk.EvidenceExcerpt,
                        DetectedAt = now,
                        DocumentId = documentId
                    };

                    dbContext.RisksDetected.Add(entity);
                    await dbContext.SaveChangesAsync();

                    risk.Id = entity.Id;
                    risk.DetectedAt = entity.DetectedAt;

                    allRisks.Add(risk);
                }
            }

            await transaction.CommitAsync();

            var uniqueRisks = allRisks
                .GroupBy(r => (r.Description?.Trim() ?? "") + "|" + (r.EvidenceExcerpt?.Trim() ?? ""))
                .Select(g => g.First())
                .ToList();

            return uniqueRisks;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<List<ExtractedEntityDto>> ExtractEntitiesAsync(int documentId)
    {
        var document = await dbContext.Documents.FindAsync(documentId) ?? throw new KeyNotFoundException($"Documento com ID {documentId} não encontrado.");

        var systemPrompt = "Você é um assistente jurídico. Extraia todas as entidades relevantes do texto fornecido. Para cada entidade, forneça: tipo (ex: Pessoa, Empresa, Data, Valor, Local, Documento, Outro), valor, score de confiança (0-1), trecho do contexto, posição inicial e final no texto. Responda em JSON com a estrutura: [{ \"EntityType\": \"Pessoa\", \"Value\": \"João Silva\", \"ConfidenceScore\": 0.98, \"ContextExcerpt\": \"...\", \"StartPosition\": 123, \"EndPosition\": 135 }]";

        var chunks = TextChunker.ChunkText(document.TextExtracted, 12000);

        var allEntities = new List<ExtractedEntityDto>();

        using var transaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            foreach (var chunk in chunks)
            {
                var response = await geminiClient.SendMessageAsync(chunk, systemPrompt, "gemini-1.5-flash");

                List<ExtractedEntityDto> entitiesChunk;
                try
                {
                    entitiesChunk = JsonSerializer.Deserialize<List<ExtractedEntityDto>>(response, _jsonOptions);
                }
                catch
                {
                    continue;
                }
                if (entitiesChunk?.Any() != true) continue;

                foreach (var dto in entitiesChunk)
                {
                    var entity = new Domain.Entities.ExtractedEntity
                    {
                        DocumentId = documentId,
                        EntityType = dto.EntityType,
                        Value = dto.Value,
                        ConfidenceScore = dto.ConfidenceScore,
                        ContextExcerpt = dto.ContextExcerpt,
                        StartPosition = dto.StartPosition,
                        EndPosition = dto.EndPosition
                    };
                    dbContext.ExtractedEntities.Add(entity);
                    await dbContext.SaveChangesAsync();

                    dto.Id = entity.Id;
                    allEntities.Add(dto);
                }
            }

            await transaction.CommitAsync();

            var uniqueEntities = allEntities
                .GroupBy(e => (e.Value?.Trim() ?? "") + "|" + e.EntityType)
                .Select(g => g.First())
                .ToList();

            return uniqueEntities;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<List<TimelineEventDto>> ExtractTimelineEventsAsync(int documentId)
    {
        var document = await dbContext.Documents.FindAsync(documentId) ?? throw new KeyNotFoundException($"Documento com ID {documentId} não encontrado.");

        var systemPrompt = "Você é um assistente jurídico. Extraia todos os eventos relevantes do texto fornecido para uma linha do tempo. Para cada evento, forneça: data do evento (EventDate, formato ISO 8601), tipo do evento (ex: Assinatura, Audiência, Sentença, Outro), descrição, trecho do texto de origem. Responda em JSON com a estrutura: [{ \"EventDate\": \"2023-01-01T00:00:00\", \"EventType\": \"Assinatura\", \"Description\": \"...\", \"SourceExcerpt\": \"...\" }]";

        var chunks = TextChunker.ChunkText(document.TextExtracted, 12000);

        var allEvents = new List<TimelineEventDto>();

        foreach (var chunk in chunks)
        {
            var response = await geminiClient.SendMessageAsync(chunk, systemPrompt, "gemini-1.5-flash");

            if (string.IsNullOrWhiteSpace(response) ||
                (!response.TrimStart().StartsWith("[") && !response.TrimStart().StartsWith("{")) ||
                response.Contains("não contém eventos", StringComparison.OrdinalIgnoreCase) ||
                response.Contains("não há eventos", StringComparison.OrdinalIgnoreCase) ||
                response.Contains("não menciona datas ou eventos", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            List<TimelineEventDto> eventsChunk;
            try
            {
                eventsChunk = JsonSerializer.Deserialize<List<TimelineEventDto>>(response, _jsonOptions);
            }
            catch
            {
                continue;
            }
            if (eventsChunk != null)
                allEvents.AddRange(eventsChunk);
        }

        var uniqueEvents = allEvents
            .GroupBy(e => (e.Description?.Trim() ?? "") + "|" + e.EventDate.ToString("o"))
            .Select(g => g.First())
            .ToList();

        return uniqueEvents;
    }

    public async Task<List<InsightDto>> GenerateInsightsAsync(int documentId)
    {
        var document = await dbContext.Documents.FindAsync(documentId) ?? throw new KeyNotFoundException($"Documento com ID {documentId} não encontrado.");

        var systemPrompt = "Você é um assistente jurídico. Gere insights relevantes a partir do texto fornecido. Para cada insight, forneça: categoria (ex: Risco, Oportunidade, Alerta, Outro), descrição, nível de importância (Low, Medium, High, Critical), trecho do texto extraído. Responda em JSON com a estrutura: [{ \"Category\": \"Risco\", \"Description\": \"...\", \"ImportanceLevel\": \"High\", \"ExtractedText\": \"...\" }]";

        var chunks = TextChunker.ChunkText(document.TextExtracted, 12000);

        var allInsights = new List<InsightDto>();

        using var transaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            foreach (var chunk in chunks)
            {
                var response = await geminiClient.SendMessageAsync(chunk, systemPrompt, "gemini-1.5-flash");

                List<InsightDto> insightsChunk;
                try
                {
                    insightsChunk = JsonSerializer.Deserialize<List<InsightDto>>(response, _jsonOptions);
                }
                catch
                {
                    continue;
                }
                if (insightsChunk?.Any() != true) continue;

                foreach (var dto in insightsChunk)
                {
                    var entity = new Domain.Entities.Insight
                    {
                        DocumentId = documentId,
                        Category = dto.Category,
                        Description = dto.Description,
                        ImportanceLevel = dto.ImportanceLevel,
                        ExtractedText = dto.ExtractedText
                    };

                    dbContext.Insights.Add(entity);
                    await dbContext.SaveChangesAsync();

                    dto.Id = entity.Id;
                    allInsights.Add(dto);
                }
            }

            await transaction.CommitAsync();

            var uniqueInsights = allInsights
                .GroupBy(i => (i.Description?.Trim() ?? "") + "|" + i.Category.ToString())
                .Select(g => g.First())
                .ToList();

            return uniqueInsights;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
