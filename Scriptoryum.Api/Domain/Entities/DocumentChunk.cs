using Pgvector;

namespace Scriptoryum.Api.Domain.Entities;

public class DocumentChunk
{
    public int Id { get; set; }
        
    public int DocumentId { get; set; }
        
    public Document Document { get; set; }
        
    public int ChunkIndex { get; set; }

    public string Content { get; set; }

    public Vector Embedding { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
