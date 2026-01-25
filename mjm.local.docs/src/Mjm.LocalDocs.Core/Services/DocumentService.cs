using Mjm.LocalDocs.Core.Abstractions;
using Mjm.LocalDocs.Core.Models;

namespace Mjm.LocalDocs.Core.Services;

/// <summary>
/// Core service for document operations.
/// Coordinates between document repository, vector store, and embedding service.
/// </summary>
public sealed class DocumentService
{
    private readonly IDocumentRepository _repository;
    private readonly IVectorStore _vectorStore;
    private readonly IDocumentProcessor _processor;
    private readonly IEmbeddingService _embeddingService;

    public DocumentService(
        IDocumentRepository repository,
        IVectorStore vectorStore,
        IDocumentProcessor processor,
        IEmbeddingService embeddingService)
    {
        _repository = repository;
        _vectorStore = vectorStore;
        _processor = processor;
        _embeddingService = embeddingService;
    }

    /// <summary>
    /// Adds a document to the store, processing it into chunks with embeddings.
    /// </summary>
    /// <param name="document">The document to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The added document.</returns>
    public async Task<Document> AddDocumentAsync(
        Document document,
        CancellationToken cancellationToken = default)
    {
        // 1. Store the document
        var savedDocument = await _repository.AddDocumentAsync(document, cancellationToken);

        // 2. Split document into chunks
        var chunks = await _processor.ChunkDocumentAsync(document, cancellationToken);

        if (chunks.Count == 0)
            return savedDocument;

        // 3. Store chunks in repository
        await _repository.AddChunksAsync(chunks, cancellationToken);

        // 4. Generate embeddings for all chunks
        var texts = chunks.Select(c => c.Content).ToList();
        var embeddings = await _embeddingService.GenerateEmbeddingsAsync(texts, cancellationToken);

        // 5. Store embeddings in vector store
        var embeddingsToStore = chunks
            .Select((chunk, index) => new KeyValuePair<string, ReadOnlyMemory<float>>(
                chunk.Id,
                embeddings[index]))
            .ToList();

        await _vectorStore.UpsertBatchAsync(embeddingsToStore, cancellationToken);

        return savedDocument;
    }

    /// <summary>
    /// Searches for documents matching the query.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="projectId">Optional project filter.</param>
    /// <param name="limit">Maximum number of results.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Search results ordered by relevance.</returns>
    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        string query,
        string? projectId = null,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        // 1. Generate embedding for query
        var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query, cancellationToken);

        // 2. Search in vector store
        var vectorResults = await _vectorStore.SearchAsync(queryEmbedding, limit * 2, cancellationToken);

        if (vectorResults.Count == 0)
            return [];

        // 3. Get chunks from repository
        var chunkIds = vectorResults.Select(r => r.ChunkId).ToList();
        var chunks = await _repository.GetChunksByIdsAsync(chunkIds, cancellationToken);

        // 4. Filter by project if specified
        if (!string.IsNullOrEmpty(projectId))
        {
            var documentsInProject = await _repository.GetDocumentsByProjectAsync(projectId, cancellationToken);
            var documentIds = documentsInProject.Select(d => d.Id).ToHashSet();
            chunks = chunks.Where(c => documentIds.Contains(c.DocumentId)).ToList();
        }

        // 5. Build search results with scores
        var chunkDict = chunks.ToDictionary(c => c.Id);
        var results = vectorResults
            .Where(vr => chunkDict.ContainsKey(vr.ChunkId))
            .Take(limit)
            .Select(vr => new SearchResult
            {
                Chunk = chunkDict[vr.ChunkId],
                Score = vr.Score
            })
            .ToList();

        return results;
    }

    /// <summary>
    /// Deletes a document and all its chunks and embeddings.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted, false if not found.</returns>
    public async Task<bool> DeleteDocumentAsync(
        string documentId,
        CancellationToken cancellationToken = default)
    {
        // 1. Delete embeddings from vector store
        await _vectorStore.DeleteByDocumentIdAsync(documentId, cancellationToken);

        // 2. Delete document (CASCADE will delete chunks)
        return await _repository.DeleteDocumentAsync(documentId, cancellationToken);
    }

    /// <summary>
    /// Gets a document by its identifier.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The document, or null if not found.</returns>
    public Task<Document?> GetDocumentAsync(
        string documentId,
        CancellationToken cancellationToken = default)
    {
        return _repository.GetDocumentAsync(documentId, cancellationToken);
    }

    /// <summary>
    /// Gets the original file content for a document.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The file content as byte array, or null if not found.</returns>
    public Task<byte[]?> GetDocumentFileAsync(
        string documentId,
        CancellationToken cancellationToken = default)
    {
        return _repository.GetDocumentFileAsync(documentId, cancellationToken);
    }

    /// <summary>
    /// Gets all documents for a project.
    /// </summary>
    /// <param name="projectId">The project identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of documents in the project.</returns>
    public Task<IReadOnlyList<Document>> GetDocumentsByProjectAsync(
        string projectId,
        CancellationToken cancellationToken = default)
    {
        return _repository.GetDocumentsByProjectAsync(projectId, cancellationToken);
    }

    /// <summary>
    /// Gets all project IDs that have documents.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of project identifiers.</returns>
    public Task<IReadOnlyList<string>> GetProjectsWithDocumentsAsync(
        CancellationToken cancellationToken = default)
    {
        return _repository.GetProjectsWithDocumentsAsync(cancellationToken);
    }
}
