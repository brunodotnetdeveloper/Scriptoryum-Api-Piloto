using Scriptoryum.Api.Domain.Enums;

namespace Scriptoryum.Api.Application.Dtos;

public class DocumentDetailsDto
{
    public int Id { get; set; }
    public string OriginalFileName { get; set; }
    public string Description { get; set; }
    public FileType FileType { get; set; }
    public string FileName { get; set; }
    public string StoragePath { get; set; }
    public long FileSize { get; set; }
    public string Status { get; set; }
    public DateTime UploadedAt { get; set; }
    public string UploadedByUserId { get; set; }

    public string TextExtracted { get; set; }
    public List<ExtractedEntityDto> ExtractedEntities { get; set; }
    public List<RiskDetectedDto> RisksDetected { get; set; }
    public List<InsightDto> Insights { get; set; }
    public List<TimelineEventDto> TimelineEvents { get; set; }
}

public class ExtractedEntityDto
{
    public int Id { get; set; }
    public string EntityType { get; set; }
    public string EntityTypeText { get; set; }
    public string Value { get; set; }
    public decimal ConfidenceScore { get; set; }
    public string ContextExcerpt { get; set; }
    public int? StartPosition { get; set; }
    public int? EndPosition { get; set; }
}

public class RiskDetectedDto
{
    public int Id { get; set; }
    public string Description { get; set; }
    public RiskLevel RiskLevel { get; set; }
    public decimal ConfidenceScore { get; set; }
    public string EvidenceExcerpt { get; set; }
    public DateTime DetectedAt { get; set; }
}

public class InsightDto
{
    public int Id { get; set; }
    public string Category { get; set; }
    public string CategoryText { get; set; }
    public string Description { get; set; }
    public ImportanceLevel ImportanceLevel { get; set; }
    public string ExtractedText { get; set; }
}

public class TimelineEventDto
{
    public int Id { get; set; }
    public DateTime EventDate { get; set; }
    public string EventType { get; set; }
    public string Description { get; set; }
    public string SourceExcerpt { get; set; }
}