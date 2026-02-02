namespace Mjm.LocalDocs.Core.Abstractions;

/// <summary>
/// Service for generating embeddings from text.
/// </summary>
public interface IEmbeddingService
{
    /// <summary>
    /// Generates an embedding vector for the given text.
    /// </summary>
    /// <param name="text">The text to embed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The embedding vector.</returns>
    Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(
        string text, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates embedding vectors for multiple texts.
    /// </summary>
    /// <param name="texts">The texts to embed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The embedding vectors in the same order as input.</returns>
    Task<IReadOnlyList<ReadOnlyMemory<float>>> GenerateEmbeddingsAsync(
        IEnumerable<string> texts, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the dimension of the embedding vectors produced by this service.
    /// </summary>
    int EmbeddingDimension { get; }
}
