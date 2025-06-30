using RestSharp;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Scriptoryum.Api.Infrastructure.Clients.OpenAI;

public class OpenAIClient(string apiKey)
{
    private readonly RestClient _client = new();

    private const string Endpoint = "https://api.openai.com/v1/chat/completions";

    public async Task<string> SendMessageAsync(string userMessage, string systemPrompt = "Você é um assistente útil.", string model = "gpt-4")
    {
        var request = new RestRequest(Endpoint, Method.Post);

        request.AddHeader("Authorization", $"Bearer {apiKey}");

        request.AddHeader("Content-Type", "application/json");

        var body = new
        {
            model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userMessage }
            },
            temperature = 0.7
        };

        request.AddJsonBody(body);

        var response = await _client.ExecuteAsync(request);

        if (!response.IsSuccessful)
            throw new Exception($"Erro na requisição: {response.StatusCode} - {response.Content}");

        var resultado = JsonSerializer.Deserialize<OpenAIResponse>(response.Content!);

        return resultado?.Choices?[0]?.Message?.Content?.Trim() ?? "Sem resposta.";
    }
}

public class OpenAIResponse
{
    [JsonPropertyName("choices")]
    public Choice[] Choices { get; set; }
}

public class Choice
{
    [JsonPropertyName("message")]
    public Message Message { get; set; }
}

public class Message
{
    [JsonPropertyName("role")]
    public string Role { get; set; }

    [JsonPropertyName("content")]
    public string Content { get; set; }
}
