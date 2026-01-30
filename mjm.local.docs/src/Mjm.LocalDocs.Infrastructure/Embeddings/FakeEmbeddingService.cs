using Mjm.LocalDocs.Core.Abstractions;

namespace Mjm.LocalDocs.Infrastructure.Embeddings;

/// <summary>
/// Fake embedding service for development and testing.
/// Generates deterministic pseudo-random embeddings based on text hash.
/// NOT suitable for production - use a real embedding provider.
/// </summary>
public sealed class FakeEmbeddingService : IEmbeddingService
{
    /// <inheritdoc />
    public int EmbeddingDimension { get; }

    public FakeEmbeddingService(int embeddingDimension = 1536)
    {
        EmbeddingDimension = embeddingDimension;
    }

    /// <inheritdoc />
    public Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(
        string text, 
        CancellationToken cancellationToken = default)
    {
        var embedding = GenerateFakeEmbedding(text);
        return Task.FromResult<ReadOnlyMemory<float>>(embedding);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ReadOnlyMemory<float>>> GenerateEmbeddingsAsync(
        IEnumerable<string> texts, 
        CancellationToken cancellationToken = default)
    {
        var embeddings = texts
            .Select(t => (ReadOnlyMemory<float>)GenerateFakeEmbedding(t))
            .ToList();
        
        return Task.FromResult<IReadOnlyList<ReadOnlyMemory<float>>>(embeddings);
    }

    /// <summary>
    /// Generates a deterministic fake embedding based on text hash.
    /// Similar texts will have similar embeddings due to word overlap scoring.
    /// </summary>
    private float[] GenerateFakeEmbedding(string text)
    {
        var embedding = new float[EmbeddingDimension];
        
        // Normalize text
        var normalizedText = text.ToLowerInvariant();
        var words = normalizedText.Split([' ', '\n', '\r', '\t', '.', ',', '!', '?'], 
            StringSplitOptions.RemoveEmptyEntries);

        // Use word hashes to populate embedding dimensions
        foreach (var word in words)
        {
            var hash = word.GetHashCode();
            var index = Math.Abs(hash) % EmbeddingDimension;
            embedding[index] += 1.0f;
        }

        // Add some character-level features
        for (int i = 0; i < normalizedText.Length && i < EmbeddingDimension; i++)
        {
            var charIndex = (normalizedText[i] * 7) % EmbeddingDimension;
            embedding[charIndex] += 0.1f;
        }

        // Normalize the vector
        var magnitude = (float)Math.Sqrt(embedding.Sum(x => x * x));
        if (magnitude > 0)
        {
            for (int i = 0; i < embedding.Length; i++)
            {
                embedding[i] /= magnitude;
            }
        }

        return embedding;
    }
}
