namespace Scriptoryum.Api.Domain.Entities;

public abstract class EntityBase
{
    public int Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; }
}
