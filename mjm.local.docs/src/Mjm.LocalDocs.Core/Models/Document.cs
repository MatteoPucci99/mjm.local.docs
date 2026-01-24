namespace Mjm.LocalDocs.Core.Models;

/// <summary>
/// Represents a document to be indexed for search.
/// </summary>
public sealed class Document
{
    /// <summary>
    /// Unique identifier for the document.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Document title or filename.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Full text content of the document.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// Collection/category for organizing documents.
    /// </summary>
    public required string Collection { get; init; }

    /// <summary>
    /// Optional metadata as key-value pairs.
    /// </summary>
    public Dictionary<string, string>? Metadata { get; init; }

    /// <summary>
    /// Timestamp when the document was added.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Timestamp when the document was last updated.
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; set; }
}
