using System.Collections.Concurrent;
using Mjm.LocalDocs.Core.Abstractions;
using Mjm.LocalDocs.Core.Models;

namespace Mjm.LocalDocs.Infrastructure.VectorStore;

/// <summary>
/// In-memory implementation of document repository for development/testing.
/// </summary>
public sealed class InMemoryDocumentRepository : IDocumentRepository
{
    private readonly ConcurrentDictionary<string, Document> _documents = new();
    private readonly ConcurrentDictionary<string, DocumentChunk> _chunks = new();

    #region Document Operations

    /// <inheritdoc />
    public Task<Document> AddDocumentAsync(
        Document document,
        CancellationToken cancellationToken = default)
    {
        _documents[document.Id] = document;
        return Task.FromResult(document);
    }

    /// <inheritdoc />
    public Task<Document?> GetDocumentAsync(
        string documentId,
        CancellationToken cancellationToken = default)
    {
        _documents.TryGetValue(documentId, out var document);
        return Task.FromResult(document);
    }

    /// <inheritdoc />
    /// <remarks>
    /// For in-memory storage, this returns the FileContent property.
    /// If the document uses external storage, FileContent will be null.
    /// </remarks>
    public Task<byte[]?> GetDocumentFileAsync(
        string documentId,
        CancellationToken cancellationToken = default)
    {
        if (_documents.TryGetValue(documentId, out var document))
        {
            return Task.FromResult(document.FileContent);
        }
        return Task.FromResult<byte[]?>(null);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Document>> GetDocumentsByProjectAsync(
        string projectId,
        CancellationToken cancellationToken = default)
    {
        var documents = _documents.Values
            .Where(d => d.ProjectId == projectId)
            .ToList();
        return Task.FromResult<IReadOnlyList<Document>>(documents);
    }

    /// <inheritdoc />
    public Task<bool> DeleteDocumentAsync(
        string documentId,
        CancellationToken cancellationToken = default)
    {
        // Delete chunks first
        var chunkKeysToRemove = _chunks
            .Where(kvp => kvp.Value.DocumentId == documentId)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in chunkKeysToRemove)
        {
            _chunks.TryRemove(key, out _);
        }

        return Task.FromResult(_documents.TryRemove(documentId, out _));
    }

    /// <inheritdoc />
    public Task<bool> DocumentExistsAsync(
        string documentId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_documents.ContainsKey(documentId));
    }

    /// <inheritdoc />
    public Task<Document?> GetDocumentByHashAsync(
        string projectId,
        string contentHash,
        CancellationToken cancellationToken = default)
    {
        var document = _documents.Values
            .FirstOrDefault(d => d.ProjectId == projectId && d.ContentHash == contentHash);
        return Task.FromResult(document);
    }

    #endregion

    #region Chunk Operations

    /// <inheritdoc />
    public Task AddChunksAsync(
        IEnumerable<DocumentChunk> chunks,
        CancellationToken cancellationToken = default)
    {
        foreach (var chunk in chunks)
        {
            _chunks[chunk.Id] = chunk;
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<DocumentChunk?> GetChunkAsync(
        string chunkId,
        CancellationToken cancellationToken = default)
    {
        _chunks.TryGetValue(chunkId, out var chunk);
        return Task.FromResult(chunk);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<DocumentChunk>> GetChunksByDocumentAsync(
        string documentId,
        CancellationToken cancellationToken = default)
    {
        var chunks = _chunks.Values
            .Where(c => c.DocumentId == documentId)
            .OrderBy(c => c.ChunkIndex)
            .ToList();
        return Task.FromResult<IReadOnlyList<DocumentChunk>>(chunks);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<DocumentChunk>> GetChunksByIdsAsync(
        IEnumerable<string> chunkIds,
        CancellationToken cancellationToken = default)
    {
        var idSet = chunkIds.ToHashSet();
        var chunks = _chunks.Values
            .Where(c => idSet.Contains(c.Id))
            .ToList();
        return Task.FromResult<IReadOnlyList<DocumentChunk>>(chunks);
    }

    /// <inheritdoc />
    public Task DeleteChunksByDocumentAsync(
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

    #endregion

    #region Project Operations

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> GetProjectsWithDocumentsAsync(
        CancellationToken cancellationToken = default)
    {
        var projectIds = _documents.Values
            .Select(d => d.ProjectId)
            .Distinct()
            .ToList();
        return Task.FromResult<IReadOnlyList<string>>(projectIds);
    }

    /// <inheritdoc />
    public Task DeleteDocumentsByProjectAsync(
        string projectId,
        CancellationToken cancellationToken = default)
    {
        var documentIds = _documents.Values
            .Where(d => d.ProjectId == projectId)
            .Select(d => d.Id)
            .ToList();

        foreach (var documentId in documentIds)
        {
            // Delete chunks
            var chunkKeysToRemove = _chunks
                .Where(kvp => kvp.Value.DocumentId == documentId)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in chunkKeysToRemove)
            {
                _chunks.TryRemove(key, out _);
            }

            // Delete document
            _documents.TryRemove(documentId, out _);
        }

        return Task.CompletedTask;
    }

    #endregion
}
