using Scriptoryum.Api.Domain.Enums;

namespace Scriptoryum.Api.Domain.Entities;

public class ExtractedEntity : EntityBase
{
    public int DocumentId { get; set; }
    public Document Document { get; set; }

    public EntityType EntityType { get; set; }
    public string Value { get; set; }
    public decimal ConfidenceScore { get; set; }

    public string ContextExcerpt { get; set; }
    public int? StartPosition { get; set; }
    public int? EndPosition { get; set; }
}