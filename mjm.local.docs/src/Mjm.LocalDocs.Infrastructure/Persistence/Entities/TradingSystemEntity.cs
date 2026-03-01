using Mjm.LocalDocs.Core.Models;

namespace Mjm.LocalDocs.Infrastructure.Persistence.Entities;

/// <summary>
/// EF Core entity for TradingSystem.
/// </summary>
public sealed class TradingSystemEntity
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? SourceUrl { get; set; }
    public required TradingSystemStatus Status { get; set; }
    public required string ProjectId { get; set; }
    public string? CodeDocumentId { get; set; }

    /// <summary>
    /// JSON array of attachment document IDs.
    /// </summary>
    public string? AttachmentDocumentIdsJson { get; set; }

    /// <summary>
    /// JSON array of tags.
    /// </summary>
    public string? TagsJson { get; set; }

    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    // Navigation properties
    public ProjectEntity? Project { get; set; }
    public DocumentEntity? CodeDocument { get; set; }
}
