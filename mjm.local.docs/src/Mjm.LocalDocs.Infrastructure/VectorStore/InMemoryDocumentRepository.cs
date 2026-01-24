using System.Collections.Concurrent;
using Mjm.LocalDocs.Core.Abstractions;
using Mjm.LocalDocs.Core.Models;

namespace Mjm.LocalDocs.Infrastructure.VectorStore;

/// <summary>
/// Simple in-memory implementation of document repository for development/testing.
/// Uses brute-force cosine similarity search.
/// </summary>
public sealed class InMemoryDocumentRepository : IDocumentRepository
{
    private readonly ConcurrentDictionary<string, DocumentChunk> _chunks = new();
    private readonly HashSet<string> _knownCollections = [];
    private readonly object _collectionLock = new();

    /// <inheritdoc />
    public Task AddChunksAsync(
        IEnumerable<DocumentChunk> chunks, 
        CancellationToken cancellationToken = default)
    {
        foreach (var chunk in chunks)
        {
            _chunks[chunk.Id] = chunk;
            
            lock (_collectionLock)
            {
                _knownCollections.Add(chunk.Collection);
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<SearchResult>> SearchAsync(
        ReadOnlyMemory<float> queryEmbedding,
        string? collection = null,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var results = new List<(DocumentChunk Chunk, double Score)>();
        
        foreach (var chunk in _chunks.Values)
        {
            if (!chunk.Embedding.HasValue)
                continue;
                
            if (!string.IsNullOrEmpty(collection) && chunk.Collection != collection)
                continue;

            var score = CosineSimilarity(queryEmbedding.Span, chunk.Embedding.Value.Span);
            results.Add((chunk, score));
        }

        var searchResults = results
            .OrderByDescending(x => x.Score)
            .Take(limit)
            .Select(x => new SearchResult
            {
                Chunk = x.Chunk,
                Score = x.Score
            })
            .ToList();

        return Task.FromResult<IReadOnlyList<SearchResult>>(searchResults);
    }

    /// <inheritdoc />
    public Task DeleteDocumentAsync(
        string documentId, 
        CancellationToken cancellationToken = default)
    {
        var keysToRemove = _chunks
            .Where(kvp => kvp.Value.DocumentId == documentId)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in keysToRemove)
        {
            _chunks.TryRemove(key, out _);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> GetCollectionsAsync(
        CancellationToken cancellationToken = default)
    {
        lock (_collectionLock)
        {
            return Task.FromResult<IReadOnlyList<string>>(_knownCollections.ToList());
        }
    }

    /// <inheritdoc />
    public Task<bool> CollectionExistsAsync(
        string collection, 
        CancellationToken cancellationToken = default)
    {
        lock (_collectionLock)
        {
            return Task.FromResult(_knownCollections.Contains(collection));
        }
    }

    /// <summary>
    /// Computes cosine similarity between two vectors.
    /// </summary>
    private static double CosineSimilarity(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
    {
        if (a.Length != b.Length)
            return 0;

        double dotProduct = 0;
        double magnitudeA = 0;
        double magnitudeB = 0;

        for (int i = 0; i < a.Length; i++)
        {
            dotProduct += a[i] * b[i];
            magnitudeA += a[i] * a[i];
            magnitudeB += b[i] * b[i];
        }

        var magnitude = Math.Sqrt(magnitudeA) * Math.Sqrt(magnitudeB);
        
        return magnitude == 0 ? 0 : dotProduct / magnitude;
    }
}
