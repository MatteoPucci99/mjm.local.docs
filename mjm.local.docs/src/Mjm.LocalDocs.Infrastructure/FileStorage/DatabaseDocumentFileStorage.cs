using Microsoft.EntityFrameworkCore;
using Mjm.LocalDocs.Core.Abstractions;
using Mjm.LocalDocs.Infrastructure.Persistence;

namespace Mjm.LocalDocs.Infrastructure.FileStorage;

/// <summary>
/// Stores document file content directly in the database (legacy/default behavior).
/// File content is stored in the Documents table's FileContent column.
/// </summary>
public sealed class DatabaseDocumentFileStorage : IDocumentFileStorage
{
    private readonly IDbContextFactory<LocalDocsDbContext> _contextFactory;

    /// <summary>
    /// Creates a new instance of DatabaseDocumentFileStorage.
    /// </summary>
    /// <param name="contextFactory">The DbContext factory for database access.</param>
    public DatabaseDocumentFileStorage(IDbContextFactory<LocalDocsDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    /// <inheritdoc />
    /// <remarks>
    /// For database storage, the file content is stored in the Documents table.
    /// This method returns a marker indicating database storage.
    /// The actual content is saved by the repository when adding the document.
    /// </remarks>
    public Task<string> SaveFileAsync(
        string documentId,
        string projectId,
        string fileName,
        byte[] content,
        CancellationToken cancellationToken = default)
    {
        // For database storage, return a special marker.
        // The actual content will be stored by EfCoreDocumentRepository.
        // We return "db://{documentId}" as the storage location.
        return Task.FromResult($"db://{documentId}");
    }

    /// <inheritdoc />
    public async Task<byte[]?> GetFileAsync(
        string documentId,
        string? storageLocation,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        
        var entity = await context.Documents
            .AsNoTracking()
            .Select(d => new { d.Id, d.FileContent })
            .FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken);

        return entity?.FileContent;
    }

    /// <inheritdoc />
    public async Task<Stream?> GetFileStreamAsync(
        string documentId,
        string? storageLocation,
        CancellationToken cancellationToken = default)
    {
        var content = await GetFileAsync(documentId, storageLocation, cancellationToken);
        return content != null ? new MemoryStream(content) : null;
    }

    /// <inheritdoc />
    /// <remarks>
    /// For database storage, file deletion is handled by EfCoreDocumentRepository
    /// when the document is deleted. This method always returns true.
    /// </remarks>
    public Task<bool> DeleteFileAsync(
        string documentId,
        string? storageLocation,
        CancellationToken cancellationToken = default)
    {
        // File content is deleted when the document is deleted from the database.
        // This is handled by EfCoreDocumentRepository.
        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public async Task<bool> FileExistsAsync(
        string documentId,
        string? storageLocation,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        
        return await context.Documents
            .AnyAsync(d => d.Id == documentId && d.FileContent != null, cancellationToken);
    }
}
