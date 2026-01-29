namespace Mjm.LocalDocs.Core.Models;

/// <summary>
/// Represents a document to be indexed for search.
/// </summary>
public sealed class Document
{
    /// <summary>
    /// Unique identifier for the document.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The project this document belongs to.
    /// </summary>
    public required string ProjectId { get; init; }

    /// <summary>
    /// Original file name (e.g., "FRD_Progetto1_v1.5.txt").
    /// </summary>
    public required string FileName { get; init; }

    /// <summary>
    /// File extension (e.g., ".txt", ".pdf", ".docx").
    /// </summary>
    public required string FileExtension { get; init; }

    /// <summary>
    /// Original file content as binary data.
    /// Null when file is stored externally (FileSystem or Azure Blob).
    /// For legacy documents, this contains the file content directly in the database.
    /// </summary>
    public byte[]? FileContent { get; init; }

    /// <summary>
    /// Storage location/path for externally stored files.
    /// Null for legacy documents where content is stored in <see cref="FileContent"/>.
    /// Format depends on storage provider:
    /// - FileSystem: relative path like "projectId/documentId.ext"
    /// - AzureBlob: blob path like "projectId/documentId.ext"
    /// - Database: null (content is in FileContent)
    /// </summary>
    public string? FileStorageLocation { get; init; }

    /// <summary>
    /// Size of the file in bytes.
    /// </summary>
    public required long FileSizeBytes { get; init; }

    /// <summary>
    /// Extracted text content used for chunking and search.
    /// </summary>
    public required string ExtractedText { get; init; }

    /// <summary>
    /// SHA256 hash of the file content for deduplication.
    /// </summary>
    public string? ContentHash { get; init; }

    /// <summary>
    /// Optional metadata as key-value pairs.
    /// </summary>
    public Dictionary<string, string>? Metadata { get; init; }

    /// <summary>
    /// Timestamp when the document was added.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Timestamp when the document was last updated.
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; set; }
}
