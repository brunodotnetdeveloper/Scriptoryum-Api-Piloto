using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Scriptoryum.Api.Application.Models;
using Scriptoryum.Api.Application.Services;
using Scriptoryum.Api.Application.Utils;

namespace Scriptoryum.Api.Application.BackgroundServices;

public class DocumentAnalysisBackgroundService : BackgroundService
{
    private readonly IBackgroundTaskQueue<DocumentAnalysisMessage> _taskQueue;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DocumentAnalysisBackgroundService> _logger;

    public DocumentAnalysisBackgroundService(
        IBackgroundTaskQueue<DocumentAnalysisMessage> taskQueue,
        IServiceProvider serviceProvider,
        ILogger<DocumentAnalysisBackgroundService> logger)
    {
        _taskQueue = taskQueue;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var message = await _taskQueue.DequeueAsync(stoppingToken);

                using var scope = _serviceProvider.CreateScope();
                var escribaService = scope.ServiceProvider.GetRequiredService<IEscribaService>();

                await escribaService.ProcessDocumentAnalysisAsync(message);
            }
            catch (OperationCanceledException)
            {
                // Graceful shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing document analysis job");
            }
        }
    }
}