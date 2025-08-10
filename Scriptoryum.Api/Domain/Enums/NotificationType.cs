namespace Scriptoryum.Api.Domain.Enums;

public enum NotificationType
{
    DocumentUploaded,
    DocumentProcessingStarted,
    DocumentTextExtracted,
    DocumentAnalysisStarted,
    DocumentAnalysisCompleted,
    DocumentProcessingFailed,
    DocumentAnalysisFailed,
    DocumentTextExtractionFailed,
    DocumentEntitiesExtractionFailed,
    DocumentRisksAnalysisFailed,
    DocumentInsightsGenerationFailed,
    SystemMaintenance,
    SecurityAlert,
    WeeklyReport,
    BillingUpdate
}