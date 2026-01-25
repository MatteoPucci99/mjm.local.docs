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
    public required byte[] FileContent { get; set; }
    public required long FileSizeBytes { get; set; }
    public required string ExtractedText { get; set; }
    public string? ContentHash { get; set; }
    public string? MetadataJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    // Navigation properties
    public ProjectEntity? Project { get; set; }
    public ICollection<DocumentChunkEntity> Chunks { get; set; } = [];
}
