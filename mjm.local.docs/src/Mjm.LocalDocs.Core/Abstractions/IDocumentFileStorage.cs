namespace Mjm.LocalDocs.Core.Abstractions;

/// <summary>
/// Abstraction for storing and retrieving document file content.
/// Implementations can store files in database, file system, Azure Blob Storage, etc.
/// </summary>
public interface IDocumentFileStorage
{
    /// <summary>
    /// Saves file content for a document.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="projectId">The project identifier (used for organizing files).</param>
    /// <param name="fileName">Original file name with extension.</param>
    /// <param name="content">The file content as byte array.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The storage location/path where the file was saved.</returns>
    Task<string> SaveFileAsync(
        string documentId,
        string projectId,
        string fileName,
        byte[] content,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets file content for a document.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="storageLocation">The storage location returned from SaveFileAsync, or null for legacy documents.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The file content as byte array, or null if not found.</returns>
    Task<byte[]?> GetFileAsync(
        string documentId,
        string? storageLocation,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets file content as a stream for large files.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="storageLocation">The storage location returned from SaveFileAsync, or null for legacy documents.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A stream to the file content, or null if not found. Caller is responsible for disposing.</returns>
    Task<Stream?> GetFileStreamAsync(
        string documentId,
        string? storageLocation,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes file content for a document.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="storageLocation">The storage location returned from SaveFileAsync, or null for legacy documents.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the file was deleted, false if not found.</returns>
    Task<bool> DeleteFileAsync(
        string documentId,
        string? storageLocation,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if file content exists for a document.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="storageLocation">The storage location returned from SaveFileAsync, or null for legacy documents.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the file exists, false otherwise.</returns>
    Task<bool> FileExistsAsync(
        string documentId,
        string? storageLocation,
        CancellationToken cancellationToken = default);
}
