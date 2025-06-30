using Scriptoryum.Api.Application.Dtos;
using Scriptoryum.Api.Application.Helpers;
using Scriptoryum.Api.Infrastructure.Clients.OpenAI;
using Scriptoryum.Api.Infrastructure.Context;
using System.Text.Json;

namespace Scriptoryum.Api.Application.Services;

public interface IEscribaService
{
    Task<DocumentAnalysisDto> AnalyzeDocument(int documentId);
    Task<List<ExtractedEntityDto>> ExtractEntitiesAsync(int documentId);
    Task<List<RiskDetectedDto>> DetectRisksAsync(int documentId);
    Task<List<InsightDto>> GenerateInsightsAsync(int documentId);
    Task<List<TimelineEventDto>> ExtractTimelineEventsAsync(int documentId);
}

public class EscribaService(OpenAIClient openAIClient, ScriptoryumDbContext dbContext) : IEscribaService
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new RiskLevelJsonConverter(), new EntityTypeJsonConverter(), new InsightCategoryJsonConverter(), new ImportanceLevelJsonConverter() }
    };

    public async Task<DocumentAnalysisDto> AnalyzeDocument(int documentId)
    {
        var document = await dbContext.Documents.FindAsync(documentId) ?? throw new KeyNotFoundException($"Documento com ID {documentId} não encontrado.");

        var analysis = new DocumentAnalysisDto
        {
            DocumentId = documentId,
            TextExtracted = document.TextExtracted,
            ExtractedEntities = await ExtractEntitiesAsync(documentId),
            DetectedRisks = await DetectRisksAsync(documentId),
            // TimelineEvents = await ExtractTimelineEventsAsync(documentId),
            GeneratedInsights = await GenerateInsightsAsync(documentId)
        };

        return analysis;
    }

    public async Task<List<RiskDetectedDto>> DetectRisksAsync(int documentId)
    {
        // Buscar documento do banco
        var document = await dbContext.Documents.FindAsync(documentId) ?? throw new KeyNotFoundException($"Documento com ID {documentId} não encontrado.");

        var systemPrompt = "Você é um assistente jurídico especializado em análise de documentos. Identifique todos os riscos presentes no texto fornecido. Para cada risco, forneça: descrição, nível de risco (Low, Medium, High, Critical), trecho do texto que evidencia o risco e um score de confiança (0-1). Responda em JSON com a estrutura: [{ \"Description\": \"...\", \"RiskLevel\": \"...\", \"ConfidenceScore\": 0.95, \"EvidenceExcerpt\": \"...\" }]";

        var chunks = TextChunker.ChunkText(document.TextExtracted, 12000);
        var allRisks = new List<RiskDetectedDto>();
        var now = DateTime.UtcNow;

        // Usar transaction scope para garantir consistência
        using var transaction = await dbContext.Database.BeginTransactionAsync();

        try
        {
            foreach (var chunk in chunks)
            {
                var response = await openAIClient.SendMessageAsync(chunk, systemPrompt);

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

                // Salvar riscos do chunk atual
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

                    // Atualizar DTO com ID gerado
                    risk.Id = entity.Id;
                    risk.DetectedAt = entity.DetectedAt;

                    allRisks.Add(risk);
                }
            }

            // Commit da transação após processar todos os chunks com sucesso
            await transaction.CommitAsync();

            // Deduplicar riscos por Description + EvidenceExcerpt
            var uniqueRisks = allRisks
                .GroupBy(r => (r.Description?.Trim() ?? "") + "|" + (r.EvidenceExcerpt?.Trim() ?? ""))
                .Select(g => g.First())
                .ToList();

            return uniqueRisks;
        }
        catch
        {
            // Rollback em caso de erro
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
                var response = await openAIClient.SendMessageAsync(chunk, systemPrompt);

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

            // Deduplicar por Value + EntityType
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
            var response = await openAIClient.SendMessageAsync(chunk, systemPrompt);

            // Verifica se a resposta indica ausência de eventos ou não é JSON
            if (string.IsNullOrWhiteSpace(response) ||
                (!response.TrimStart().StartsWith("[") && !response.TrimStart().StartsWith("{")) ||
                response.Contains("não contém eventos", StringComparison.OrdinalIgnoreCase) ||
                response.Contains("não há eventos", StringComparison.OrdinalIgnoreCase) ||
                response.Contains("não menciona datas ou eventos", StringComparison.OrdinalIgnoreCase))
            {
                // Opcional: logar resposta ignorada
                // Console.WriteLine($"[TimelineEvents] Ignored response: {response}");
                continue;
            }

            List<TimelineEventDto> eventsChunk;
            try
            {
                eventsChunk = JsonSerializer.Deserialize<List<TimelineEventDto>>(response, _jsonOptions);
            }
            catch
            {
                // Opcional: logar erro de desserialização
                // Console.WriteLine($"[TimelineEvents] JSON error: {response}");
                continue;
            }
            if (eventsChunk != null)
                allEvents.AddRange(eventsChunk);
        }

        // Deduplicar por Description + EventDate
        var uniqueEvents = allEvents
            .GroupBy(e => (e.Description?.Trim() ?? "") + "|" + e.EventDate.ToString("o"))
            .Select(g => g.First())
            .ToList();

        // Persistir no banco (se houver tabela/entidade para eventos extraídos)
        // Exemplo: dbContext.TimelineEvents.Add(entity); await dbContext.SaveChangesAsync();
        // Se não houver persistência, apenas retorna os DTOs

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
                var response = await openAIClient.SendMessageAsync(chunk, systemPrompt);

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

            // Deduplicar por Description + Category
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
