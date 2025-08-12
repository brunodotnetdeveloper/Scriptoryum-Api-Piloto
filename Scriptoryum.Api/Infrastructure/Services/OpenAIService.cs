using System.Text;
using System.Text.Json;
using System.Diagnostics;

namespace Scriptoryum.Api.Infrastructure.Services;

public class OpenAIService : IOpenAIService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenAIService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public OpenAIService(HttpClient httpClient, ILogger<OpenAIService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = false
        };
    }

    public async Task<OpenAIResponse> GenerateResponseAsync(OpenAIRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {request.ApiKey}");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Scriptoryum/1.0");

            var messages = new List<object>
            {
                new { role = "system", content = "Você é o Escriba, um assistente de IA especializado em análise de documentos. Você ajuda usuários a entender, analisar e extrair insights de seus documentos. Seja preciso, útil e forneça respostas bem estruturadas." }
            };

            if (!string.IsNullOrEmpty(request.Context))
            {
                messages.Add(new { role = "system", content = $"Contexto do documento:\n{request.Context}" });
            }

            messages.Add(new { role = "user", content = request.Message });

            var payload = new
            {
                model = request.Model,
                messages = messages,
                temperature = request.Temperature,
                max_tokens = request.MaxTokens,
                stream = false
            };

            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("Enviando requisição para OpenAI - Model: {Model}, Tokens: {MaxTokens}", 
                request.Model, request.MaxTokens);

            var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            stopwatch.Stop();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Erro na API OpenAI: {StatusCode} - {Content}", 
                    response.StatusCode, responseContent);
                
                return new OpenAIResponse
                {
                    Success = false,
                    ErrorMessage = $"API Error: {response.StatusCode}",
                    ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds
                };
            }

            var openAIResponse = JsonSerializer.Deserialize<OpenAIApiResponse>(responseContent, _jsonOptions);
            
            if (openAIResponse?.Choices?.FirstOrDefault()?.Message?.Content == null)
            {
                return new OpenAIResponse
                {
                    Success = false,
                    ErrorMessage = "Resposta inválida da API OpenAI",
                    ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds
                };
            }

            var tokensUsed = openAIResponse.Usage?.TotalTokens ?? 0;
            var cost = CalculateCost(request.Model, tokensUsed);

            _logger.LogInformation("Resposta OpenAI recebida - Tokens: {Tokens}, Custo: ${Cost}, Tempo: {Time}ms", 
                tokensUsed, cost, stopwatch.ElapsedMilliseconds);

            return new OpenAIResponse
            {
                Response = openAIResponse.Choices.First().Message.Content,
                TokensUsed = tokensUsed,
                Cost = cost,
                Model = request.Model,
                ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds,
                Success = true
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Erro ao chamar API OpenAI");
            
            return new OpenAIResponse
            {
                Success = false,
                ErrorMessage = ex.Message,
                ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds
            };
        }
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text, string apiKey)
    {
        try
        {
            // Usa o token do modelo selecionado pelo usuário (passado como parâmetro)
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            var payload = new
            {
                model = "text-embedding-3-small",
                input = text
            };

            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("https://api.openai.com/v1/embeddings", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Erro ao gerar embedding: {StatusCode} - {Content}", 
                    response.StatusCode, responseContent);
                return Array.Empty<float>();
            }

            var embeddingResponse = JsonSerializer.Deserialize<OpenAIEmbeddingResponse>(responseContent, _jsonOptions);
            return embeddingResponse?.Data?.FirstOrDefault()?.Embedding ?? Array.Empty<float>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar embedding");
            return Array.Empty<float>();
        }
    }

    private static decimal CalculateCost(string model, int tokens)
    {
        // Custos por 1K tokens (valores aproximados)
        var costs = new Dictionary<string, decimal>
        {
            ["gpt-4o"] = 0.005m,
            ["gpt-4o-mini"] = 0.00015m,
            ["gpt-4-turbo"] = 0.01m,
            ["gpt-3.5-turbo"] = 0.0005m
        };

        if (costs.TryGetValue(model, out var costPer1k))
        {
            return (tokens / 1000m) * costPer1k;
        }

        return 0m;
    }
}