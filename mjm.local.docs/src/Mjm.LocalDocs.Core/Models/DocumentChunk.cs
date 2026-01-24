namespace Mjm.LocalDocs.Core.Models;

/// <summary>
/// Represents a chunk of a document with its embedding for vector search.
/// </summary>
public sealed class DocumentChunk
{
    /// <summary>
    /// Unique identifier for the chunk.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The text content of the chunk.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// The source document identifier.
    /// </summary>
    public required string DocumentId { get; init; }

    /// <summary>
    /// The collection/category this chunk belongs to.
    /// </summary>
    public required string Collection { get; init; }

    /// <summary>
    /// Original document title or filename.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Position of this chunk within the original document.
    /// </summary>
    public int ChunkIndex { get; init; }

    /// <summary>
    /// Timestamp when the chunk was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// The embedding vector for semantic search.
    /// </summary>
    public ReadOnlyMemory<float>? Embedding { get; set; }
}
