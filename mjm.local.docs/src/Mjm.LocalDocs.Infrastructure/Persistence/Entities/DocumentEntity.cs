namespace Mjm.LocalDocs.Infrastructure.Persistence.Entities;

/// <summary>
/// EF Core entity for Document.
/// </summary>
public sealed class DocumentEntity
{
    public required string Id { get; set; }
    public required string ProjectId { get; set; }
    public required string FileName { get; set; }
    public required string FileExtension { get; set; }
    /// <summary>
    /// Original file content. Null when stored externally (FileSystem or Azure Blob).
    /// </summary>
    public byte[]? FileContent { get; set; }
    /// <summary>
    /// Storage location/path for externally stored files.
    /// Null for legacy documents where content is stored in FileContent.
    /// </summary>
    public string? FileStorageLocation { get; set; }
    public required long FileSizeBytes { get; set; }
    public required string ExtractedText { get; set; }
    public string? ContentHash { get; set; }
    public string? MetadataJson { get; set; }
    public int VersionNumber { get; set; }
    public string? ParentDocumentId { get; set; }
    public bool IsSuperseded { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    // Navigation properties
    public ProjectEntity? Project { get; set; }
    public ICollection<DocumentChunkEntity> Chunks { get; set; } = [];
}
