namespace Mjm.LocalDocs.Core.Models;

/// <summary>
/// Represents a search result from vector search.
/// </summary>
public sealed class SearchResult
{
    /// <summary>
    /// The matching document chunk.
    /// </summary>
    public required DocumentChunk Chunk { get; init; }

    /// <summary>
    /// Relevance score (0-1, higher is more relevant).
    /// </summary>
    public double Score { get; init; }
}
