using Scriptoryum.Api.Domain.Enums;

namespace Scriptoryum.Api.Domain.Entities;

public class Document : EntityBase
{
    public string OriginalFileName { get; set; }
    public string ProcessedFileName { get; set; }
    public string Summary { get; set; }
    public string Description { get; set; }

    public StorageProvider StorageProvider { get; set; }
    public string StoragePath { get; set; }
    public FileType FileType { get; set; }
    public long FileSize { get; set; }

    public string UploadedByUserId { get; set; }
    public ApplicationUser UploadedByUser { get; set; }
    
    // Workspace association
    public int? WorkspaceId { get; set; }
    public Workspace Workspace { get; set; }
    
    public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;
    public DocumentStatus Status { get; set; } = DocumentStatus.Uploaded;

    public string TextExtracted { get; set; }

    public DateTimeOffset? ProcessingStartedAt { get; set; }

    public ICollection<ExtractedEntity> ExtractedEntities { get; set; } = [];

    public ICollection<Insight> Insights { get; set; } = [];
    public ICollection<RiskDetected> RisksDetected { get; set; } = [];
    public ICollection<TimelineEvent> TimelineEvents { get; set; } = [];
}
