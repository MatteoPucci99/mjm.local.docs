using Mjm.LocalDocs.Core.Abstractions;
using Mjm.LocalDocs.Core.Models;

namespace Mjm.LocalDocs.Core.Services;

/// <summary>
/// Core service for document operations.
/// </summary>
public sealed class DocumentService
{
    private readonly IDocumentRepository _repository;
    private readonly IDocumentProcessor _processor;
    private readonly IEmbeddingService _embeddingService;

    public DocumentService(
        IDocumentRepository repository,
        IDocumentProcessor processor,
        IEmbeddingService embeddingService)
    {
        _repository = repository;
        _processor = processor;
        _embeddingService = embeddingService;
    }

    /// <summary>
    /// Adds a document to the store, processing it into chunks with embeddings.
    /// </summary>
    public async Task AddDocumentAsync(
        Document document, 
        CancellationToken cancellationToken = default)
    {
        // Split document into chunks
        var chunks = await _processor.ChunkDocumentAsync(document, cancellationToken);
        
        if (chunks.Count == 0)
            return;

        // Generate embeddings for all chunks
        var texts = chunks.Select(c => c.Content).ToList();
        var embeddings = await _embeddingService.GenerateEmbeddingsAsync(texts, cancellationToken);

        // Assign embeddings to chunks
        var chunksWithEmbeddings = chunks
            .Select((chunk, index) =>
            {
                chunk.Embedding = embeddings[index];
                return chunk;
            })
            .ToList();

        // Store chunks
        await _repository.AddChunksAsync(chunksWithEmbeddings, cancellationToken);
    }

    /// <summary>
    /// Searches for documents matching the query.
    /// </summary>
    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        string query,
        string? collection = null,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        // Generate embedding for query
        var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query, cancellationToken);

        // Search in repository
        return await _repository.SearchAsync(queryEmbedding, collection, limit, cancellationToken);
    }

    /// <summary>
    /// Deletes a document and all its chunks.
    /// </summary>
    public async Task DeleteDocumentAsync(
        string documentId, 
        CancellationToken cancellationToken = default)
    {
        await _repository.DeleteDocumentAsync(documentId, cancellationToken);
    }

    /// <summary>
    /// Gets all available collections.
    /// </summary>
    public Task<IReadOnlyList<string>> GetCollectionsAsync(
        CancellationToken cancellationToken = default)
    {
        return _repository.GetCollectionsAsync(cancellationToken);
    }
}
