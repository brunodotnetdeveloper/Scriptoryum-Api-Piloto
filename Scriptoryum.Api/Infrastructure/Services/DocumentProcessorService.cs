//using Scriptoryum.Api.Application.Dtos;
//using Scriptoryum.Api.Infrastructure.Context;
//using Scriptoryum.Api.Domain.Enums;
//using Microsoft.EntityFrameworkCore;

//namespace Scriptoryum.Api.Infrastructure.Services;

//public interface IDocumentProcessorService
//{
//    Task ProcessDocumentsAsync(CancellationToken cancellationToken = default);
//}

//public class DocumentProcessorService : IDocumentProcessorService
//{
//    private readonly IRedisQueueService _redisQueueService;
//    private readonly ScriptoryumDbContext _context;
//    private readonly ILogger<DocumentProcessorService> _logger;
//    private const string QueueName = "document-processing-queue";

//    public DocumentProcessorService(
//        IRedisQueueService redisQueueService,
//        ScriptoryumDbContext context,
//        ILogger<DocumentProcessorService> logger)
//    {
//        _redisQueueService = redisQueueService;
//        _context = context;
//        _logger = logger;
//    }

//    public async Task ProcessDocumentsAsync(CancellationToken cancellationToken = default)
//    {
//        _logger.LogInformation("Iniciando processamento de documentos da fila Redis");

//        while (!cancellationToken.IsCancellationRequested)
//        {
//            try
//            {
//                var documentDto = await _redisQueueService.DequeueDocumentAsync<DocumentQueueDto>(QueueName);
                
//                if (documentDto == null)
//                {
//                    // Não há documentos na fila, aguarda um pouco antes de verificar novamente
//                    await Task.Delay(5000, cancellationToken);
//                    continue;
//                }

//                await ProcessDocumentAsync(documentDto);
//            }
//            catch (OperationCanceledException)
//            {
//                _logger.LogInformation("Processamento de documentos cancelado");
//                break;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Erro durante o processamento de documentos");
//                await Task.Delay(10000, cancellationToken); // Aguarda mais tempo em caso de erro
//            }
//        }
//    }

//    private async Task ProcessDocumentAsync(DocumentQueueDto documentDto)
//    {
//        try
//        {
//            _logger.LogInformation("Processando documento {DocumentId}", documentDto.DocumentId);

//            // Buscar o documento no banco de dados
//            var document = await _context.Documents
//                .FirstOrDefaultAsync(d => d.Id == documentDto.DocumentId);

//            if (document == null)
//            {
//                _logger.LogWarning("Documento {DocumentId} não encontrado no banco de dados", documentDto.DocumentId);
//                return;
//            }

//            // Atualizar status para "Em Processamento"
//            document.Status = DocumentStatus.Queued;
//            await _context.SaveChangesAsync();

//            // Aqui você pode adicionar a lógica de processamento do documento
//            // Por exemplo: extração de texto, análise de conteúdo, etc.
//            await SimulateDocumentProcessing(document);

//            // Atualizar status para "Concluído"
//            document.Status = DocumentStatus.Processed;
//            await _context.SaveChangesAsync();

//            _logger.LogInformation("Documento {DocumentId} processado com sucesso", documentDto.DocumentId);
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Erro ao processar documento {DocumentId}", documentDto.DocumentId);
            
//            // Atualizar status para "Erro"
//            try
//            {
//                var document = await _context.Documents
//                    .FirstOrDefaultAsync(d => d.Id == documentDto.DocumentId);
                
//                if (document != null)
//                {
//                    document.Status = DocumentStatus.Failed;
//                    await _context.SaveChangesAsync();
//                }
//            }
//            catch (Exception saveEx)
//            {
//                _logger.LogError(saveEx, "Erro ao atualizar status do documento {DocumentId} para erro", documentDto.DocumentId);
//            }
//        }
//    }

//    private async Task SimulateDocumentProcessing(Domain.Entities.Document document)
//    {
//        // Simula processamento do documento
//        _logger.LogInformation("Simulando processamento do documento {DocumentId} - {FileName}", 
//            document.Id, document.OriginalFileName);
        
//        // Simula tempo de processamento baseado no tipo de arquivo
//        var processingTime = document.FileType switch
//        {
//            FileType.PDF => TimeSpan.FromSeconds(10),
//            FileType.DOCX => TimeSpan.FromSeconds(8),
//            FileType.TXT => TimeSpan.FromSeconds(3),
//            _ => TimeSpan.FromSeconds(5)
//        };

//        await Task.Delay(processingTime);
        
//        _logger.LogInformation("Processamento simulado concluído para documento {DocumentId}", document.Id);
//    }
//}