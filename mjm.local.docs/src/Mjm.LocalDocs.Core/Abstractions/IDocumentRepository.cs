using Mjm.LocalDocs.Core.Models;

namespace Mjm.LocalDocs.Core.Abstractions;

/// <summary>
/// Repository for storing and retrieving document chunks.
/// </summary>
public interface IDocumentRepository
{
    /// <summary>
    /// Adds document chunks to the store.
    /// </summary>
    /// <param name="chunks">The chunks to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddChunksAsync(
        IEnumerable<DocumentChunk> chunks, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for chunks similar to the query embedding.
    /// </summary>
    /// <param name="queryEmbedding">The query embedding vector.</param>
    /// <param name="collection">Optional collection filter.</param>
    /// <param name="limit">Maximum number of results.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Search results ordered by relevance.</returns>
    Task<IReadOnlyList<SearchResult>> SearchAsync(
        ReadOnlyMemory<float> queryEmbedding,
        string? collection = null,
        int limit = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all chunks for a given document.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteDocumentAsync(
        string documentId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all available collections.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of collection names.</returns>
    Task<IReadOnlyList<string>> GetCollectionsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a collection exists.
    /// </summary>
    /// <param name="collection">The collection name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<bool> CollectionExistsAsync(
        string collection, 
        CancellationToken cancellationToken = default);
}
