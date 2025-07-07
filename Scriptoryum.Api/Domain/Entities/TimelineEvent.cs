using Scriptoryum.Api.Domain.Enums;

namespace Scriptoryum.Api.Domain.Entities;

public class TimelineEvent: EntityBase
{
    public int DocumentId { get; set; }
    public Document Document { get; set; }

    public DateTime EventDate { get; set; }
    public TimelineEventType EventType { get; set; }
    public string Description { get; set; }
    public string SourceExcerpt { get; set; }
}