using StackExchange.Redis;
using System.Text.Json;

namespace Scriptoryum.Api.Infrastructure.Services;

public interface IRedisQueueService
{
    Task EnqueueDocumentAsync<T>(string queueName, T data) where T : class;
    Task<T> DequeueDocumentAsync<T>(string queueName) where T : class;
    Task<long> GetQueueLengthAsync(string queueName);
}

public class RedisQueueService : IRedisQueueService
{
    private readonly IDatabase _database;
    private readonly ILogger<RedisQueueService> _logger;
    private readonly bool _isRedisAvailable;

    public RedisQueueService(IConnectionMultiplexer connectionMultiplexer, ILogger<RedisQueueService> logger)
    {
        _logger = logger;
        
        if (connectionMultiplexer != null && connectionMultiplexer.IsConnected)
        {
            _database = connectionMultiplexer.GetDatabase();
            _isRedisAvailable = true;
            _logger.LogInformation("RedisQueueService initialized with active Redis connection");
        }
        else
        {
            _database = null;
            _isRedisAvailable = false;
            _logger.LogWarning("RedisQueueService initialized without Redis connection - queue operations will be skipped");
        }
    }

    public async Task EnqueueDocumentAsync<T>(string queueName, T data) where T : class
    {
        if (!_isRedisAvailable || _database == null)
        {
            _logger.LogWarning("Redis not available - skipping enqueue operation for {QueueName}", queueName);
            return;
        }

        try
        {
            var json = JsonSerializer.Serialize(data);
            await _database.ListLeftPushAsync(queueName, json);
            _logger.LogInformation("Item adicionado à fila {QueueName}", queueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao adicionar item à fila {QueueName}", queueName);
            throw;
        }
    }

    public async Task<T> DequeueDocumentAsync<T>(string queueName) where T : class
    {
        if (!_isRedisAvailable || _database == null)
        {
            _logger.LogWarning("Redis not available - skipping dequeue operation for {QueueName}", queueName);
            return null;
        }

        try
        {
            var json = await _database.ListRightPopAsync(queueName);
            if (!json.HasValue)
                return null;

            var data = JsonSerializer.Deserialize<T>(json!);
            _logger.LogInformation("Item removido da fila {QueueName}", queueName);
            return data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao remover item da fila {QueueName}", queueName);
            throw;
        }
    }

    public async Task<long> GetQueueLengthAsync(string queueName)
    {
        if (!_isRedisAvailable || _database == null)
        {
            _logger.LogWarning("Redis not available - returning 0 for queue length of {QueueName}", queueName);
            return 0;
        }

        try
        {
            return await _database.ListLengthAsync(queueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter tamanho da fila {QueueName}", queueName);
            throw;
        }
    }
}