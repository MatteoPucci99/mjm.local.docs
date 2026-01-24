using Microsoft.Extensions.AI;
using Mjm.LocalDocs.Core.Abstractions;

namespace Mjm.LocalDocs.Infrastructure.Embeddings;

/// <summary>
/// Embedding service implementation using Microsoft.Extensions.AI abstractions.
/// Works with any IEmbeddingGenerator provider (OpenAI, Ollama, etc.).
/// </summary>
public sealed class SemanticKernelEmbeddingService : IEmbeddingService
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;

    public SemanticKernelEmbeddingService(
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        int embeddingDimension = 1536)
    {
        _embeddingGenerator = embeddingGenerator;
        EmbeddingDimension = embeddingDimension;
    }

    /// <inheritdoc />
    public int EmbeddingDimension { get; }

    /// <inheritdoc />
    public async Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(
        string text, 
        CancellationToken cancellationToken = default)
    {
        var result = await _embeddingGenerator.GenerateAsync(
            [text], 
            cancellationToken: cancellationToken);
        
        return result[0].Vector;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ReadOnlyMemory<float>>> GenerateEmbeddingsAsync(
        IEnumerable<string> texts, 
        CancellationToken cancellationToken = default)
    {
        var textList = texts.ToList();
        
        var result = await _embeddingGenerator.GenerateAsync(
            textList, 
            cancellationToken: cancellationToken);

        return result.Select(e => e.Vector).ToList();
    }
}
