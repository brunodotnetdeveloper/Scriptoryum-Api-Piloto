using RestSharp;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Scriptoryum.Api.Infrastructure.Clients.Gemini;

public class GeminiClient(string apiKey)
{
    private readonly RestClient _client = new();

    public async Task<string> SendMessageAsync(
        string userMessage,
        string systemPrompt = "Você é um assistente útil.",
        string model = "gemini-1.5-flash")
    {
        // Build endpoint dynamically for the requested model
        var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";
        var request = new RestRequest(endpoint, Method.Post);

        request.AddHeader("Content-Type", "application/json");

        var body = new
        {
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[]
                    {
                        new { text = $"{systemPrompt}\n{userMessage}" }
                    }
                }
            }
        };

        request.AddJsonBody(body);

        var response = await _client.ExecuteAsync(request);

        if (!response.IsSuccessful)
            throw new Exception($"Erro na requisição Gemini: {response.StatusCode} - {response.Content}");

        var resultado = JsonSerializer.Deserialize<GeminiResponse>(response.Content!);

        return resultado?.candidates?.FirstOrDefault()?.content?.parts?.FirstOrDefault()?.text?.Trim() ?? "Sem resposta.";
    }
}

public class GeminiResponse
{
    [JsonPropertyName("candidates")]
    public GeminiCandidate[] candidates { get; set; }
}

public class GeminiCandidate
{
    [JsonPropertyName("content")]
    public GeminiContent content { get; set; }
}

public class GeminiContent
{
    [JsonPropertyName("parts")]
    public GeminiPart[] parts { get; set; }
}

public class GeminiPart
{
    [JsonPropertyName("text")]
    public string text { get; set; }
}
