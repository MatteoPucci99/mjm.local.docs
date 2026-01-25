namespace Mjm.LocalDocs.Infrastructure.Persistence.Entities;

/// <summary>
/// EF Core entity for DocumentChunk.
/// Embedding is stored separately in sqlite-vec virtual table.
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
