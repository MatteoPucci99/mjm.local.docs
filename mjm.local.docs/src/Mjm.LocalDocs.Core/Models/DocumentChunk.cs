namespace Mjm.LocalDocs.Core.Models;

/// <summary>
/// Represents a chunk of a document for vector search.
/// The embedding is stored separately in the vector store.
/// </summary>
public sealed class DocumentChunk
{
    /// <summary>
    /// Unique identifier for the chunk.
    /// Format: {DocumentId}_chunk_{ChunkIndex}
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
    /// Original document file name.
    /// </summary>
    public string? FileName { get; init; }

    /// <summary>
    /// Position of this chunk within the original document.
    /// </summary>
    public int ChunkIndex { get; init; }

    /// <summary>
    /// Timestamp when the chunk was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
