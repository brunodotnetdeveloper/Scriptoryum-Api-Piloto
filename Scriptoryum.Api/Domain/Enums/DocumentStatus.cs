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
    PartiallyProcessed,
    EntitiesExtractionFailed,
    RisksAnalysisFailed,
    InsightsGenerationFailed,
    Analyzed
}