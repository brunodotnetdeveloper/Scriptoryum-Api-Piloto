using Scriptoryum.Api.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Scriptoryum.Api.Application.Dtos;

public class DocumentsDto
{
    public int Id { get; set; }
    public string OriginalFileName { get; set; }
    public string Description { get; set; }
    public FileType FileType { get; set; }
    public string FileName { get; set; }
    public string StoragePath { get; set; }
    public long FileSize { get; set; }
    public string Status { get; set; }
    public DateTimeOffset UploadedAt { get; set; }

    public string UploadedByUserId { get; set; }
}

public class UploadDocumentDto
{
    [Required]
    public IFormFile File { get; set; }
    
    [StringLength(1000)]
    public string Description { get; set; }
    
    public int? WorkspaceId { get; set; }
}

public class UploadDocumentResponseDto
{
    public int DocumentId { get; set; }
    public string Message { get; set; }
    public bool Success { get; set; }
}
