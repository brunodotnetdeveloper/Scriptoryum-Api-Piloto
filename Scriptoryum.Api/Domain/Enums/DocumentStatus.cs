namespace Scriptoryum.Api.Domain.Enums;

public enum DocumentStatus
{
    Uploaded,
    Queued,
    ExtractingText,
    AnalyzingContent,
    Processed,
    TextExtractionFailed,
    ContentAnalysisFailed,
    Failed,
    Cancelled,
    // New statuses for partial failures
    PartiallyProcessed,
    EntitiesExtractionFailed,
    RisksAnalysisFailed,
    InsightsGenerationFailed
}