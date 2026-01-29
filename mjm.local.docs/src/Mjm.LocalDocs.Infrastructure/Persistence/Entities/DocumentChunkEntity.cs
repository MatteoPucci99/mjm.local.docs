namespace Mjm.LocalDocs.Infrastructure.Persistence.Entities;

/// <summary>
/// EF Core entity for DocumentChunk.
/// Embeddings are stored separately (sqlite-vec for SQLite, chunk_embeddings table for SQL Server).
/// </summary>
public sealed class DocumentChunkEntity
{
    public required string Id { get; set; }
    public required string DocumentId { get; set; }
    public required string Content { get; set; }
    public string? FileName { get; set; }
    public int ChunkIndex { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    // Navigation property
    public DocumentEntity? Document { get; set; }
}
