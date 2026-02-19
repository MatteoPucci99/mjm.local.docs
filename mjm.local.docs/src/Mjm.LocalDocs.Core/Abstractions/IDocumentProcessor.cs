using Mjm.LocalDocs.Core.Models;

namespace Mjm.LocalDocs.Core.Abstractions;

/// <summary>
/// Service for processing documents into chunks.
/// </summary>
public interface IDocumentProcessor
{
    /// <summary>
    /// Splits a document into chunks suitable for embedding.
    /// </summary>
    /// <param name="document">The document to process.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The document chunks (without embeddings).</returns>
    Task<IReadOnlyList<DocumentChunk>> ChunkDocumentAsync(
        Document document, 
        CancellationToken cancellationToken = default);
}
