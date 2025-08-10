using Scriptoryum.Api.Domain.Enums;

namespace Scriptoryum.Api.Application.Dtos;

public class DocumentQueueDto
{
    public int DocumentId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string ProcessedFileName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public FileType FileType { get; set; }
    public string UploadedByUserId { get; set; } = string.Empty;
    public DateTimeOffset UploadedAt { get; set; }
    public DateTimeOffset QueuedAt { get; set; }
}