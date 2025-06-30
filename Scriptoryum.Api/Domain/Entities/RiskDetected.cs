using Scriptoryum.Api.Domain.Enums;

namespace Scriptoryum.Api.Domain.Entities;

public class RiskDetected: EntityBase
{
    public int DocumentId { get; set; }
    public Document Document { get; set; }

    public string Description { get; set; }
    public RiskLevel RiskLevel { get; set; }
    public decimal ConfidenceScore { get; set; }

    public string EvidenceExcerpt { get; set; }
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
}
