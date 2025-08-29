using System.ComponentModel.DataAnnotations;
using Scriptoryum.Api.Domain.Enums;

namespace Scriptoryum.Api.Application.Dtos;

public class ChatSessionDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; }
    public int? DocumentId { get; set; }
    public string DocumentName { get; set; }
    public int MessageCount { get; set; }
    public DateTimeOffset LastActivityAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public List<ChatMessageDto> Messages { get; set; } = [];
}

public class ChatMessageDto
{
    public int Id { get; set; }
    public int ChatSessionId { get; set; }
    public string Role { get; set; }
    public string Content { get; set; } = string.Empty;
    public int? DocumentId { get; set; }
    public string DocumentName { get; set; }
    public int? TokenCount { get; set; }
    public decimal? Cost { get; set; }
    public AIProvider? AIProvider { get; set; }
    public string ModelUsed { get; set; }
    public int? ResponseTimeMs { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class CreateChatSessionDto
{
    [StringLength(200, ErrorMessage = "Título deve ter no máximo 200 caracteres")]
    public string Title { get; set; }
    
    [StringLength(500, ErrorMessage = "Descrição deve ter no máximo 500 caracteres")]
    public string Description { get; set; }
    
    public int? DocumentId { get; set; }
}

public class UpdateChatSessionDto
{
    [Required(ErrorMessage = "Título é obrigatório")]
    [StringLength(200, ErrorMessage = "Título deve ter no máximo 200 caracteres")]
    public string Title { get; set; } = string.Empty;
    
    [StringLength(500, ErrorMessage = "Descrição deve ter no máximo 500 caracteres")]
    public string Description { get; set; }
}

public class SendMessageDto
{
    [Required(ErrorMessage = "Mensagem é obrigatória")]
    [StringLength(10000, ErrorMessage = "Mensagem deve ter no máximo 10000 caracteres")]
    public string Message { get; set; } = string.Empty;
    
    public int? SessionId { get; set; }
    public int? DocumentId { get; set; }
    public string Context { get; set; }
}

public class SendMessageResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? SessionId { get; set; }
    public int? MessageId { get; set; }
    public string Response { get; set; }
    public List<string>? Suggestions { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class DocumentContextDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<object>? Entities { get; set; }
    public List<object>? Insights { get; set; }
    public List<object>? Risks { get; set; }
}

public class GetSuggestionsDto
{
    public string Context { get; set; }
}

public class AnalyzeDocumentDto
{
    [Required(ErrorMessage = "ID do documento é obrigatório")]
    public int DocumentId { get; set; }
    
    [Required(ErrorMessage = "Tipo de análise é obrigatório")]
    public string AnalysisType { get; set; } = string.Empty;
}

public class CompareDocumentsDto
{
    [Required(ErrorMessage = "IDs dos documentos são obrigatórios")]
    [MinLength(2, ErrorMessage = "Pelo menos 2 documentos são necessários para comparação")]
    public List<int> DocumentIds { get; set; } = new();
}

public class SearchDocumentsDto
{
    [Required(ErrorMessage = "Query de busca é obrigatória")]
    [StringLength(500, ErrorMessage = "Query deve ter no máximo 500 caracteres")]
    public string Query { get; set; } = string.Empty;
    
    public List<int>? DocumentIds { get; set; }
}

public class ChatSessionResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public ChatSessionDto Session { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class ChatSessionsResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<ChatSessionDto> Sessions { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

public class DocumentContextResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public DocumentContextDto? Context { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class SuggestionsResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> Suggestions { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

public class AnalysisResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Analysis { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class SearchResultsResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<object> Results { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}