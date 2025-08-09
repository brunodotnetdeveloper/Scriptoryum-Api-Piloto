namespace Scriptoryum.Api.Infrastructure.Services;

public interface IRagService
{
    Task<RagContext> GetRelevantContextAsync(string query, string userId, int? documentId = null, int maxChunks = 5);
    Task<List<DocumentChunkResult>> SearchSimilarChunksAsync(string query, string userId, int? documentId = null, int maxResults = 10);
}

public class RagContext
{
    public string Context { get; set; } = string.Empty;
    public List<DocumentChunkResult> RelevantChunks { get; set; } = new();
    public int TotalChunks { get; set; }
    public double AverageScore { get; set; }
}

public class DocumentChunkResult
{
    public int ChunkId { get; set; }
    public int DocumentId { get; set; }
    public string DocumentName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int ChunkIndex { get; set; }
    public double SimilarityScore { get; set; }
}