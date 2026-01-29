using Mjm.LocalDocs.Core.Abstractions;
using Mjm.LocalDocs.Core.Configuration;
using Mjm.LocalDocs.Core.Models;

namespace Mjm.LocalDocs.Core.Services;

/// <summary>
/// Core service for document operations.
/// Coordinates between document repository, vector store, embedding service, and file storage.
/// </summary>
public sealed class DocumentService
{
    private readonly IDocumentRepository _repository;
    private readonly IVectorStore _vectorStore;
    private readonly IDocumentProcessor _processor;
    private readonly IEmbeddingService _embeddingService;
    private readonly IDocumentFileStorage? _fileStorage;
    private readonly FileStorageProvider _fileStorageProvider;

    /// <summary>
    /// Creates a new DocumentService.
    /// </summary>
    /// <param name="repository">Document repository.</param>
    /// <param name="vectorStore">Vector store for embeddings.</param>
    /// <param name="processor">Document processor for chunking.</param>
    /// <param name="embeddingService">Embedding service.</param>
    /// <param name="fileStorage">Optional file storage for external file content storage.</param>
    /// <param name="fileStorageProvider">The configured file storage provider type.</param>
    public DocumentService(
        IDocumentRepository repository,
        IVectorStore vectorStore,
        IDocumentProcessor processor,
        IEmbeddingService embeddingService,
        IDocumentFileStorage? fileStorage = null,
        FileStorageProvider fileStorageProvider = FileStorageProvider.Database)
    {
        _repository = repository;
        _vectorStore = vectorStore;
        _processor = processor;
        _embeddingService = embeddingService;
        _fileStorage = fileStorage;
        _fileStorageProvider = fileStorageProvider;
    }

    /// <summary>
    /// Adds a document to the store, processing it into chunks with embeddings.
    /// File content is stored according to the configured FileStorageProvider.
    /// </summary>
    /// <param name="document">The document to add. Must have FileContent set.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The added document with FileStorageLocation set if using external storage.</returns>
    /// <exception cref="ArgumentException">Thrown when document.FileContent is null.</exception>
    public async Task<Document> AddDocumentAsync(
        Document document,
        CancellationToken cancellationToken = default)
    {
        if (document.FileContent == null)
        {
            throw new ArgumentException("FileContent is required when adding a document.", nameof(document));
        }

        // 1. Store file content based on configured provider
        Document documentToStore;
        string? storageLocation = null;
        
        if (_fileStorageProvider != FileStorageProvider.Database && _fileStorage != null)
        {
            // Store file externally (FileSystem or AzureBlob)
            storageLocation = await _fileStorage.SaveFileAsync(
                document.Id,
                document.ProjectId,
                document.FileName,
                document.FileContent,
                cancellationToken);

            // Create document without inline content, with storage location
            documentToStore = new Document
            {
                Id = document.Id,
                ProjectId = document.ProjectId,
                FileName = document.FileName,
                FileExtension = document.FileExtension,
                FileContent = null, // Don't store in database
                FileStorageLocation = storageLocation,
                FileSizeBytes = document.FileSizeBytes,
                ExtractedText = document.ExtractedText,
                ContentHash = document.ContentHash,
                Metadata = document.Metadata,
                CreatedAt = document.CreatedAt,
                UpdatedAt = document.UpdatedAt
            };
        }
        else
        {
            // Store file content in database (default/legacy behavior)
            documentToStore = document;
        }

        // 2. Store the document metadata (and content if using database storage)
        Document savedDocument;
        try
        {
            savedDocument = await _repository.AddDocumentAsync(documentToStore, cancellationToken);
        }
        catch
        {
            // Cleanup external file if database operation fails
            if (storageLocation != null && _fileStorage != null)
            {
                await _fileStorage.DeleteFileAsync(document.Id, storageLocation, cancellationToken);
            }
            throw;
        }

        // 3. Split document into chunks (uses ExtractedText, not FileContent)
        var chunks = await _processor.ChunkDocumentAsync(document, cancellationToken);

        if (chunks.Count == 0)
            return savedDocument;

        // 4. Store chunks in repository
        await _repository.AddChunksAsync(chunks, cancellationToken);

        // 5. Generate embeddings for all chunks
        var texts = chunks.Select(c => c.Content).ToList();
        var embeddings = await _embeddingService.GenerateEmbeddingsAsync(texts, cancellationToken);

        // 6. Store embeddings in vector store
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
    /// Deletes a document and all its chunks, embeddings, and external file content.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted, false if not found.</returns>
    public async Task<bool> DeleteDocumentAsync(
        string documentId,
        CancellationToken cancellationToken = default)
    {
        // 1. Get document to check for external file storage
        var document = await _repository.GetDocumentAsync(documentId, cancellationToken);
        
        // 2. Delete external file if using external storage
        if (document != null && _fileStorage != null && !string.IsNullOrEmpty(document.FileStorageLocation))
        {
            await _fileStorage.DeleteFileAsync(documentId, document.FileStorageLocation, cancellationToken);
        }

        // 3. Delete embeddings from vector store
        await _vectorStore.DeleteByDocumentIdAsync(documentId, cancellationToken);

        // 4. Delete document (CASCADE will delete chunks)
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
    /// Retrieves from the appropriate storage location (database or external).
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The file content as byte array, or null if not found.</returns>
    public async Task<byte[]?> GetDocumentFileAsync(
        string documentId,
        CancellationToken cancellationToken = default)
    {
        // First get the document to check storage location
        var document = await _repository.GetDocumentAsync(documentId, cancellationToken);
        if (document == null)
        {
            return null;
        }

        // If content is stored inline (legacy or database storage)
        if (document.FileContent != null)
        {
            return document.FileContent;
        }

        // If using external storage
        if (_fileStorage != null && !string.IsNullOrEmpty(document.FileStorageLocation))
        {
            return await _fileStorage.GetFileAsync(documentId, document.FileStorageLocation, cancellationToken);
        }

        // Fallback: try repository (for database storage without inline content)
        return await _repository.GetDocumentFileAsync(documentId, cancellationToken);
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
