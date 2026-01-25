using Mjm.LocalDocs.Core.Models;

namespace Mjm.LocalDocs.Core.Abstractions;

/// <summary>
/// Repository for storing and retrieving documents and their chunks.
/// </summary>
public interface IDocumentRepository
{
    #region Document Operations

    /// <summary>
    /// Adds a new document.
    /// </summary>
    /// <param name="document">The document to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The added document.</returns>
    Task<Document> AddDocumentAsync(
        Document document,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a document by its identifier.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The document, or null if not found.</returns>
    Task<Document?> GetDocumentAsync(
        string documentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the original file content for a document.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The file content as byte array, or null if not found.</returns>
    Task<byte[]?> GetDocumentFileAsync(
        string documentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all documents for a project.
    /// </summary>
    /// <param name="projectId">The project identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of documents in the project.</returns>
    Task<IReadOnlyList<Document>> GetDocumentsByProjectAsync(
        string projectId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a document and all its chunks.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted, false if not found.</returns>
    Task<bool> DeleteDocumentAsync(
        string documentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a document exists.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if exists, false otherwise.</returns>
    Task<bool> DocumentExistsAsync(
        string documentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a document with the same content hash exists in the project.
    /// </summary>
    /// <param name="projectId">The project identifier.</param>
    /// <param name="contentHash">The SHA256 hash of the file content.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The existing document if found, null otherwise.</returns>
    Task<Document?> GetDocumentByHashAsync(
        string projectId,
        string contentHash,
        CancellationToken cancellationToken = default);

    #endregion

    #region Chunk Operations

    /// <summary>
    /// Adds document chunks to the store.
    /// </summary>
    /// <param name="chunks">The chunks to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddChunksAsync(
        IEnumerable<DocumentChunk> chunks,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a chunk by its identifier.
    /// </summary>
    /// <param name="chunkId">The chunk identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The chunk, or null if not found.</returns>
    Task<DocumentChunk?> GetChunkAsync(
        string chunkId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all chunks for a document.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of chunks for the document.</returns>
    Task<IReadOnlyList<DocumentChunk>> GetChunksByDocumentAsync(
        string documentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets chunks by their identifiers.
    /// </summary>
    /// <param name="chunkIds">The chunk identifiers.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of chunks found.</returns>
    Task<IReadOnlyList<DocumentChunk>> GetChunksByIdsAsync(
        IEnumerable<string> chunkIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all chunks for a document.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteChunksByDocumentAsync(
        string documentId,
        CancellationToken cancellationToken = default);

    #endregion

    #region Project Operations

    /// <summary>
    /// Gets all project IDs that have documents.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of project identifiers.</returns>
    Task<IReadOnlyList<string>> GetProjectsWithDocumentsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all documents and chunks for a project.
    /// </summary>
    /// <param name="projectId">The project identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteDocumentsByProjectAsync(
        string projectId,
        CancellationToken cancellationToken = default);

    #endregion
}
