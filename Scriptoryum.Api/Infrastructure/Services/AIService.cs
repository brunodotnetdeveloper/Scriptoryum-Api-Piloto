using System.Text;
using System.Text.Json;
using System.Diagnostics;
using Scriptoryum.Api.Domain.Enums;

namespace Scriptoryum.Api.Infrastructure.Services;

public class AIService : IAIService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AIService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public AIService(HttpClient httpClient, ILogger<AIService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = false
        };
    }

    public async Task<AIResponse> GenerateResponseAsync(AIRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            return request.Provider switch
            {
                AIProvider.OpenAI => await GenerateOpenAIResponseAsync(request, stopwatch),
                AIProvider.Claude => await GenerateClaudeResponseAsync(request, stopwatch),
                AIProvider.Gemini => await GenerateGeminiResponseAsync(request, stopwatch),
                _ => throw new NotSupportedException($"Provedor {request.Provider} não é suportado")
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Erro ao chamar API {Provider}", request.Provider);
            
            return new AIResponse
            {
                Success = false,
                ErrorMessage = ex.Message,
                ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds,
                Provider = request.Provider
            };
        }
    }

    private async Task<AIResponse> GenerateOpenAIResponseAsync(AIRequest request, Stopwatch stopwatch)
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
            
            return new AIResponse
            {
                Success = false,
                ErrorMessage = $"API Error: {response.StatusCode}",
                ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds,
                Provider = AIProvider.OpenAI
            };
        }

        var openAIResponse = JsonSerializer.Deserialize<OpenAIApiResponse>(responseContent, _jsonOptions);
        
        if (openAIResponse?.Choices?.FirstOrDefault()?.Message?.Content == null)
        {
            return new AIResponse
            {
                Success = false,
                ErrorMessage = "Resposta inválida da API OpenAI",
                ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds,
                Provider = AIProvider.OpenAI
            };
        }

        var tokensUsed = openAIResponse.Usage?.TotalTokens ?? 0;
        var cost = CalculateOpenAICost(request.Model, tokensUsed);

        _logger.LogInformation("Resposta OpenAI recebida - Tokens: {Tokens}, Custo: ${Cost}, Tempo: {Time}ms", 
            tokensUsed, cost, stopwatch.ElapsedMilliseconds);

        return new AIResponse
        {
            Response = openAIResponse.Choices.First().Message.Content,
            TokensUsed = tokensUsed,
            Cost = cost,
            Model = request.Model,
            ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds,
            Success = true,
            Provider = AIProvider.OpenAI
        };
    }

    private async Task<AIResponse> GenerateClaudeResponseAsync(AIRequest request, Stopwatch stopwatch)
    {
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("x-api-key", request.ApiKey);
        _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Scriptoryum/1.0");

        var systemMessage = "Você é o Escriba, um assistente de IA especializado em análise de documentos. Você ajuda usuários a entender, analisar e extrair insights de seus documentos. Seja preciso, útil e forneça respostas bem estruturadas.";
        
        if (!string.IsNullOrEmpty(request.Context))
        {
            systemMessage += $"\n\nContexto do documento:\n{request.Context}";
        }

        var payload = new
        {
            model = request.Model,
            max_tokens = request.MaxTokens,
            temperature = request.Temperature,
            system = systemMessage,
            messages = new[]
            {
                new { role = "user", content = request.Message }
            }
        };

        var json = JsonSerializer.Serialize(payload, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _logger.LogInformation("Enviando requisição para Claude - Model: {Model}, Tokens: {MaxTokens}", 
            request.Model, request.MaxTokens);

        var response = await _httpClient.PostAsync("https://api.anthropic.com/v1/messages", content);
        var responseContent = await response.Content.ReadAsStringAsync();

        stopwatch.Stop();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Erro na API Claude: {StatusCode} - {Content}", 
                response.StatusCode, responseContent);
            
            return new AIResponse
            {
                Success = false,
                ErrorMessage = $"API Error: {response.StatusCode}",
                ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds,
                Provider = AIProvider.Claude
            };
        }

        var claudeResponse = JsonSerializer.Deserialize<ClaudeApiResponse>(responseContent, _jsonOptions);
        
        if (claudeResponse?.Content?.FirstOrDefault()?.Text == null)
        {
            return new AIResponse
            {
                Success = false,
                ErrorMessage = "Resposta inválida da API Claude",
                ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds,
                Provider = AIProvider.Claude
            };
        }

        var tokensUsed = (claudeResponse.Usage?.InputTokens ?? 0) + (claudeResponse.Usage?.OutputTokens ?? 0);
        var cost = CalculateClaudeCost(request.Model, claudeResponse.Usage?.InputTokens ?? 0, claudeResponse.Usage?.OutputTokens ?? 0);

        _logger.LogInformation("Resposta Claude recebida - Tokens: {Tokens}, Custo: ${Cost}, Tempo: {Time}ms", 
            tokensUsed, cost, stopwatch.ElapsedMilliseconds);

        return new AIResponse
        {
            Response = claudeResponse.Content.First().Text,
            TokensUsed = tokensUsed,
            Cost = cost,
            Model = request.Model,
            ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds,
            Success = true,
            Provider = AIProvider.Claude
        };
    }

    private async Task<AIResponse> GenerateGeminiResponseAsync(AIRequest request, Stopwatch stopwatch)
    {
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Scriptoryum/1.0");

        var systemInstruction = "Você é o Escriba, um assistente de IA especializado em análise de documentos. Você ajuda usuários a entender, analisar e extrair insights de seus documentos. Seja preciso, útil e forneça respostas bem estruturadas.";
        
        if (!string.IsNullOrEmpty(request.Context))
        {
            systemInstruction += $"\n\nContexto do documento:\n{request.Context}";
        }

        var payload = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = request.Message }
                    }
                }
            },
            systemInstruction = new
            {
                parts = new[]
                {
                    new { text = systemInstruction }
                }
            },
            generationConfig = new
            {
                temperature = request.Temperature,
                maxOutputTokens = request.MaxTokens
            }
        };

        var json = JsonSerializer.Serialize(payload, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _logger.LogInformation("Enviando requisição para Gemini - Model: {Model}, Tokens: {MaxTokens}", 
            request.Model, request.MaxTokens);

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{request.Model}:generateContent?key={request.ApiKey}";
        var response = await _httpClient.PostAsync(url, content);
        var responseContent = await response.Content.ReadAsStringAsync();

        stopwatch.Stop();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Erro na API Gemini: {StatusCode} - {Content}", 
                response.StatusCode, responseContent);
            
            return new AIResponse
            {
                Success = false,
                ErrorMessage = $"API Error: {response.StatusCode}",
                ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds,
                Provider = AIProvider.Gemini
            };
        }

        var geminiResponse = JsonSerializer.Deserialize<GeminiApiResponse>(responseContent, _jsonOptions);
        
        if (geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text == null)
        {
            return new AIResponse
            {
                Success = false,
                ErrorMessage = "Resposta inválida da API Gemini",
                ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds,
                Provider = AIProvider.Gemini
            };
        }

        var tokensUsed = (geminiResponse.UsageMetadata?.PromptTokenCount ?? 0) + (geminiResponse.UsageMetadata?.CandidatesTokenCount ?? 0);
        var cost = CalculateGeminiCost(request.Model, tokensUsed);

        _logger.LogInformation("Resposta Gemini recebida - Tokens: {Tokens}, Custo: ${Cost}, Tempo: {Time}ms", 
            tokensUsed, cost, stopwatch.ElapsedMilliseconds);

        return new AIResponse
        {
            Response = geminiResponse.Candidates.First().Content.Parts.First().Text,
            TokensUsed = tokensUsed,
            Cost = cost,
            Model = request.Model,
            ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds,
            Success = true,
            Provider = AIProvider.Gemini
        };
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text, string apiKey, AIProvider provider)
    {
        try
        {
            return provider switch
            {
                AIProvider.OpenAI => await GenerateOpenAIEmbeddingAsync(text, apiKey),
                _ => throw new NotSupportedException($"Embeddings não suportados para o provedor {provider}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar embedding com {Provider}", provider);
            return Array.Empty<float>();
        }
    }

    private async Task<float[]> GenerateOpenAIEmbeddingAsync(string text, string apiKey)
    {
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

    private static decimal CalculateOpenAICost(string model, int tokens)
    {
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

    private static decimal CalculateClaudeCost(string model, int inputTokens, int outputTokens)
    {
        var costs = new Dictionary<string, (decimal input, decimal output)>
        {
            ["claude-3-5-sonnet-20241022"] = (0.003m, 0.015m),
            ["claude-3-haiku-20240307"] = (0.00025m, 0.00125m)
        };

        if (costs.TryGetValue(model, out var cost))
        {
            return (inputTokens / 1000m) * cost.input + (outputTokens / 1000m) * cost.output;
        }

        return 0m;
    }

    private static decimal CalculateGeminiCost(string model, int tokens)
    {
        var costs = new Dictionary<string, decimal>
        {
            ["gemini-1.5-pro"] = 0.00125m,
            ["gemini-1.5-flash"] = 0.000075m
        };

        if (costs.TryGetValue(model, out var costPer1k))
        {
            return (tokens / 1000m) * costPer1k;
        }

        return 0m;
    }
}

// Classes para deserialização das respostas
public class OpenAIApiResponse
{
    public List<Choice>? Choices { get; set; }
    public Usage? Usage { get; set; }
}

public class Choice
{
    public Message? Message { get; set; }
}

public class Message
{
    public string Content { get; set; }
}

public class Usage
{
    public int TotalTokens { get; set; }
}

public class OpenAIEmbeddingResponse
{
    public List<EmbeddingData>? Data { get; set; }
}

public class EmbeddingData
{
    public float[]? Embedding { get; set; }
}

// Classes para Claude
public class ClaudeApiResponse
{
    public List<ClaudeContent>? Content { get; set; }
    public ClaudeUsage? Usage { get; set; }
}

public class ClaudeContent
{
    public string Text { get; set; }
}

public class ClaudeUsage
{
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
}

// Classes para Gemini
public class GeminiApiResponse
{
    public List<GeminiCandidate>? Candidates { get; set; }
    public GeminiUsageMetadata? UsageMetadata { get; set; }
}

public class GeminiCandidate
{
    public GeminiContent? Content { get; set; }
}

public class GeminiContent
{
    public List<GeminiPart>? Parts { get; set; }
}

public class GeminiPart
{
    public string Text { get; set; }
}

public class GeminiUsageMetadata
{
    public int PromptTokenCount { get; set; }
    public int CandidatesTokenCount { get; set; }
}