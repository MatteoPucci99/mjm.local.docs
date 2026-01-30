using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Mjm.LocalDocs.Core.Abstractions;
using Mjm.LocalDocs.Core.Models;
using Mjm.LocalDocs.Infrastructure.Persistence.Entities;

namespace Mjm.LocalDocs.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of document repository.
/// Supports SQLite, SQL Server, and other EF Core providers.
/// </summary>
public sealed class EfCoreDocumentRepository : IDocumentRepository
{
    private readonly LocalDocsDbContext _context;

    public EfCoreDocumentRepository(LocalDocsDbContext context)
    {
        _context = context;
    }

    #region Document Operations

    /// <inheritdoc />
    public async Task<Document> AddDocumentAsync(
        Document document,
        CancellationToken cancellationToken = default)
    {
        var entity = new DocumentEntity
        {
            Id = document.Id,
            ProjectId = document.ProjectId,
            FileName = document.FileName,
            FileExtension = document.FileExtension,
            FileContent = document.FileContent,
            FileStorageLocation = document.FileStorageLocation,
            FileSizeBytes = document.FileSizeBytes,
            ExtractedText = document.ExtractedText,
            ContentHash = document.ContentHash,
            MetadataJson = document.Metadata != null
                ? JsonSerializer.Serialize(document.Metadata)
                : null,
            CreatedAt = document.CreatedAt,
            UpdatedAt = document.UpdatedAt
        };

        _context.Documents.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return document;
    }

    /// <inheritdoc />
    public async Task<Document?> GetDocumentAsync(
        string documentId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.Documents
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken);

        return entity == null ? null : MapToModel(entity);
    }

    /// <inheritdoc />
    public async Task<byte[]?> GetDocumentFileAsync(
        string documentId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.Documents
            .AsNoTracking()
            .Select(d => new { d.Id, d.FileContent })
            .FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken);

        return entity?.FileContent;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Document>> GetDocumentsByProjectAsync(
        string projectId,
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.Documents
            .AsNoTracking()
            .Where(d => d.ProjectId == projectId)
            .OrderBy(d => d.FileName)
            .ToListAsync(cancellationToken);

        return entities.Select(MapToModel).ToList();
    }

    /// <inheritdoc />
    public async Task<bool> DeleteDocumentAsync(
        string documentId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken);

        if (entity == null)
        {
            return false;
        }

        _context.Documents.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> DocumentExistsAsync(
        string documentId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Documents
            .AnyAsync(d => d.Id == documentId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Document?> GetDocumentByHashAsync(
        string projectId,
        string contentHash,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.Documents
            .AsNoTracking()
            .FirstOrDefaultAsync(
                d => d.ProjectId == projectId && d.ContentHash == contentHash,
                cancellationToken);

        return entity == null ? null : MapToModel(entity);
    }

    #endregion

    #region Chunk Operations

    /// <inheritdoc />
    public async Task AddChunksAsync(
        IEnumerable<DocumentChunk> chunks,
        CancellationToken cancellationToken = default)
    {
        var entities = chunks.Select(c => new DocumentChunkEntity
        {
            Id = c.Id,
            DocumentId = c.DocumentId,
            Content = c.Content,
            FileName = c.FileName,
            ChunkIndex = c.ChunkIndex,
            CreatedAt = c.CreatedAt
        }).ToList();

        _context.DocumentChunks.AddRange(entities);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DocumentChunk?> GetChunkAsync(
        string chunkId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.DocumentChunks
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == chunkId, cancellationToken);

        return entity == null ? null : MapToChunkModel(entity);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DocumentChunk>> GetChunksByDocumentAsync(
        string documentId,
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.DocumentChunks
            .AsNoTracking()
            .Where(c => c.DocumentId == documentId)
            .OrderBy(c => c.ChunkIndex)
            .ToListAsync(cancellationToken);

        return entities.Select(MapToChunkModel).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DocumentChunk>> GetChunksByIdsAsync(
        IEnumerable<string> chunkIds,
        CancellationToken cancellationToken = default)
    {
        var idList = chunkIds.ToList();

        var entities = await _context.DocumentChunks
            .AsNoTracking()
            .Where(c => idList.Contains(c.Id))
            .ToListAsync(cancellationToken);

        return entities.Select(MapToChunkModel).ToList();
    }

    /// <inheritdoc />
    public async Task DeleteChunksByDocumentAsync(
        string documentId,
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.DocumentChunks
            .Where(c => c.DocumentId == documentId)
            .ToListAsync(cancellationToken);

        _context.DocumentChunks.RemoveRange(entities);
        await _context.SaveChangesAsync(cancellationToken);
    }

    #endregion

    #region Project Operations

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetProjectsWithDocumentsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.Documents
            .AsNoTracking()
            .Select(d => d.ProjectId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteDocumentsByProjectAsync(
        string projectId,
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.Documents
            .Where(d => d.ProjectId == projectId)
            .ToListAsync(cancellationToken);

        _context.Documents.RemoveRange(entities);
        await _context.SaveChangesAsync(cancellationToken);
    }

    #endregion

    #region Private Helpers

    private static Document MapToModel(DocumentEntity entity)
    {
        return new Document
        {
            Id = entity.Id,
            ProjectId = entity.ProjectId,
            FileName = entity.FileName,
            FileExtension = entity.FileExtension,
            FileContent = entity.FileContent,
            FileStorageLocation = entity.FileStorageLocation,
            FileSizeBytes = entity.FileSizeBytes,
            ExtractedText = entity.ExtractedText,
            ContentHash = entity.ContentHash,
            Metadata = !string.IsNullOrEmpty(entity.MetadataJson)
                ? JsonSerializer.Deserialize<Dictionary<string, string>>(entity.MetadataJson)
                : null,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    private static DocumentChunk MapToChunkModel(DocumentChunkEntity entity)
    {
        return new DocumentChunk
        {
            Id = entity.Id,
            DocumentId = entity.DocumentId,
            Content = entity.Content,
            FileName = entity.FileName,
            ChunkIndex = entity.ChunkIndex,
            CreatedAt = entity.CreatedAt
        };
    }

    #endregion
}
