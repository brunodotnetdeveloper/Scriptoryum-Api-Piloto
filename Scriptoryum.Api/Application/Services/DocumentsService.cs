using Microsoft.EntityFrameworkCore;
using Scriptoryum.Api.Application.Dtos;
using Scriptoryum.Api.Domain.Entities;
using Scriptoryum.Api.Domain.Enums;
using Scriptoryum.Api.Infrastructure.Clients;
using Scriptoryum.Api.Infrastructure.Context;
using Scriptoryum.Api.Infrastructure.Services;

namespace Scriptoryum.Api.Application.Services;

public interface IDocumentsService
{
    Task<IEnumerable<DocumentsDto>> GetDocumentsByUserAsync(string userId);
    Task<UploadDocumentResponseDto> UploadDocumentAsync(UploadDocumentDto uploadDto, string userId);
    Task<DocumentDetailsDto> GetDocumentDetailsByIdAsync(int id);
    Task<string> GetDocumentDownloadUrlAsync(string storagePath, TimeSpan expiration);
}

public class DocumentsService(ScriptoryumDbContext context, ILogger<DocumentsService> logger, ICloudflareR2Client r2Client, IRedisQueueService redisQueueService, INotificationService notificationService) : IDocumentsService
{
    private readonly Dictionary<string, FileType> _allowedExtensions = new()
    {
        { ".pdf", FileType.PDF },
        { ".docx", FileType.DOCX },
        { ".doc", FileType.DOC },
        { ".txt", FileType.TXT },
        { ".rtf", FileType.RTF },
        { ".odt", FileType.ODT },
        { ".html", FileType.HTML },
        { ".htm", FileType.HTML },
        { ".xml", FileType.XML },
        { ".xls", FileType.XLS },
        { ".xlsx", FileType.XLSX },
        { ".json", FileType.JSON }
    };

    private const long MaxFileSize = 50 * 1024 * 1024; // 50MB

    public async Task<IEnumerable<DocumentsDto>> GetDocumentsByUserAsync(string userId)
    {
        var documents = await context.Documents
            .Where(d => d.UploadedByUserId == userId)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync();

        return documents.Select(MapToDto);
    }

    public async Task<UploadDocumentResponseDto> UploadDocumentAsync(UploadDocumentDto uploadDto, string userId)
    {
        try
        {
            // Validar arquivo
            var validationResult = ValidateFile(uploadDto.File);

            if (!validationResult.IsValid)
            {
                return new UploadDocumentResponseDto
                {
                    Success = false,
                    Message = validationResult.ErrorMessage
                };
            }

            // Gerar nome único para o arquivo
            var fileExtension = Path.GetExtension(uploadDto.File.FileName).ToLowerInvariant();
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            var objectName = $"documents/{uniqueFileName}";

            // Upload para Cloudflare R2 usando stream diretamente (mais eficiente para arquivos grandes)
            bool uploadSuccess = false;
            using (var fileStream = uploadDto.File.OpenReadStream())
            {
                // Upload para o Cloudflare R2 usando SDK AWS S3
                uploadSuccess = await r2Client.UploadFileAsync(fileStream, objectName, uploadDto.File.ContentType);
            }

            if (!uploadSuccess)
            {
                return new UploadDocumentResponseDto
                {
                    Success = false,
                    Message = "Erro ao fazer upload do arquivo para o storage"
                };
            }

            // Criar entidade Document
            var document = new Document
            {
                OriginalFileName= Path.GetFileNameWithoutExtension(uploadDto.File.FileName),
                ProcessedFileName = uniqueFileName,
                Description = uploadDto.Description,
                StorageProvider = StorageProvider.CloudflareR2,
                StoragePath = objectName,
                FileType = _allowedExtensions[fileExtension],
                FileSize = uploadDto.File.Length,
                UploadedByUserId = userId,
                UploadedAt = DateTime.UtcNow,
                Status = DocumentStatus.Uploaded
            };

            context.Documents.Add(document);

            await context.SaveChangesAsync();

            // Documento salvo com sucesso
            logger.LogInformation("Documento {DocumentId} salvo com sucesso", document.Id);

            // Criar notificação de upload
            try
            {
                await notificationService.CreateNotificationAsync(new CreateNotificationDto
                {
                    UserId = userId,
                    Type = NotificationType.DocumentUploaded,
                    Title = "Documento enviado com sucesso",
                    Message = $"O documento '{document.OriginalFileName}' foi enviado e está sendo processado.",
                    DocumentId = document.Id
                });
                logger.LogInformation("Notificação de upload criada para documento {DocumentId}", document.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao criar notificação de upload para documento {DocumentId}", document.Id);
                // Não falha a operação se a notificação falhar
            }

            // Enviar documento para fila Redis para processamento
            try
            {
                var documentQueueDto = new DocumentQueueDto
                {
                    DocumentId = document.Id,
                    OriginalFileName = document.OriginalFileName,
                    ProcessedFileName = document.ProcessedFileName,
                    Description = document.Description,
                    StoragePath = document.StoragePath,
                    FileType = document.FileType,
                    UploadedByUserId = document.UploadedByUserId,
                    UploadedAt = document.UploadedAt,
                    QueuedAt = DateTime.UtcNow
                };

                await redisQueueService.EnqueueDocumentAsync("document-processing-queue", documentQueueDto);
                logger.LogInformation("Documento {DocumentId} adicionado à fila de processamento", document.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao adicionar documento {DocumentId} à fila Redis. O documento foi salvo mas não foi enfileirado.", document.Id);
                // Não falha a operação se a fila falhar, pois o documento já foi salvo
            }

            logger.LogInformation("Documento {DocumentId} enviado com sucesso por usuário {UserId}", document.Id, userId);

            return new UploadDocumentResponseDto
            {
                DocumentId = document.Id,
                Success = true,
                Message = "Documento enviado com sucesso"
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao fazer upload do documento para usuário {UserId}", userId);
            return new UploadDocumentResponseDto
            {
                Success = false,
                Message = "Erro interno do servidor ao processar o arquivo"
            };
        }
    }

    public async Task<string> GetDocumentDownloadUrlAsync(string storagePath, TimeSpan expiration)
    {
        if (string.IsNullOrEmpty(storagePath)) return null;
        return await r2Client.GetPresignedUrlAsync(storagePath, expiration);
    }

    public async Task<DocumentDetailsDto> GetDocumentDetailsByIdAsync(int id)
    {
        // Primeiro, carrega o documento
        var document = await context.Documents
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id);

        if (document == null) return null;

        // Depois carrega cada relacionamento sequencialmente
        var entities = await context.ExtractedEntities
            .AsNoTracking()
            .Where(e => e.DocumentId == id)
            .ToListAsync();

        var risks = await context.RisksDetected
            .AsNoTracking()
            .Where(r => r.DocumentId == id)
            .ToListAsync();

        var insights = await context.Insights
            .AsNoTracking()
            .Where(i => i.DocumentId == id)
            .ToListAsync();

        var timeline = await context.TimelineEvents
            .AsNoTracking()
            .Where(t => t.DocumentId == id)
            .ToListAsync();

        return new DocumentDetailsDto
        {
            Id = document.Id,
            OriginalFileName = document.OriginalFileName,
            Description = document.Description,
            FileType = document.FileType,
            FileName = Path.GetFileName(document.StoragePath),
            StoragePath = document.StoragePath,
            FileSize = document.FileSize,
            Status = document.Status.ToString(),
            UploadedAt = document.UploadedAt,
            UploadedByUserId = document.UploadedByUserId,
            TextExtracted = document.TextExtracted,

            ExtractedEntities = entities.Select(e => new ExtractedEntityDto
            {
                Id = e.Id,
                EntityType = e.EntityType,
                EntityTypeText = e.EntityType.ToString(),
                Value = e.Value,
                ConfidenceScore = e.ConfidenceScore,
                ContextExcerpt = e.ContextExcerpt,
                StartPosition = e.StartPosition,
                EndPosition = e.EndPosition
            }).ToList(),

            RisksDetected = risks.Select(r => new RiskDetectedDto
            {
                Id = r.Id,
                Description = r.Description,
                RiskLevel = r.RiskLevel,
                ConfidenceScore = r.ConfidenceScore,
                EvidenceExcerpt = r.EvidenceExcerpt,
                DetectedAt = r.DetectedAt
            }).ToList(),

            Insights = insights.Select(i => new InsightDto
            {
                Id = i.Id,
                Category = i.Category,
                CategoryText = i.Category.ToString(),
                Description = i.Description,
                ImportanceLevel = i.ImportanceLevel,
                ExtractedText = i.ExtractedText
            }).ToList(),

            TimelineEvents = timeline.Select(t => new TimelineEventDto
            {
                Id = t.Id,
                EventDate = t.EventDate,
                EventType = t.EventType,
                Description = t.Description,
                SourceExcerpt = t.SourceExcerpt
            }).ToList()
        };
    }

    private static (bool IsValid, string ErrorMessage) ValidateFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return (false, "Nenhum arquivo foi selecionado");

        if (file.Length > MaxFileSize)
            return (false, $"Arquivo muito grande. Tamanho máximo permitido: {MaxFileSize / (1024 * 1024)}MB");

        return (true, string.Empty);
    }

    private DocumentsDto MapToDto(Document document)
    {
        return new DocumentsDto
        {
            Id = document.Id,
            OriginalFileName = document.OriginalFileName,
            Description = document.Description,
            FileType = document.FileType,
            FileName = Path.GetFileName(document.StoragePath),
            StoragePath = document.StoragePath,
            FileSize = document.FileSize,
            Status = document.Status.ToString(),
            UploadedAt = document.UploadedAt,
            UploadedByUserId = document.UploadedByUserId
        };
    }
}
