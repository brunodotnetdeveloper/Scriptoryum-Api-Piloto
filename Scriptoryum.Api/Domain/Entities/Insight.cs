using Scriptoryum.Api.Domain.Enums;

namespace Scriptoryum.Api.Domain.Entities;

public class Insight: EntityBase
{
    public int DocumentId { get; set; }
    public Document Document { get; set; }

    public string Category { get; set; }
    public string Description { get; set; }
    public ImportanceLevel ImportanceLevel { get; set; }
    public string ExtractedText { get; set; }
}
