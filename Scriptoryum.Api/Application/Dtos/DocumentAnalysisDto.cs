namespace Scriptoryum.Api.Application.Dtos;

public class DocumentAnalysisDto
{
    public int DocumentId { get; set; }
    public string FileName { get; set; }
    public string TextExtracted { get; set; }
    public List<ExtractedEntityDto> ExtractedEntities { get; set; } = [];
    public List<RiskDetectedDto> DetectedRisks { get; set; } = [];
    public List<InsightDto> GeneratedInsights { get; set; } = [];
    public List<TimelineEventDto> TimelineEvents { get; set; } = [];
}
