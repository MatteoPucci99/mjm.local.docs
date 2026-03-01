namespace Mjm.LocalDocs.Infrastructure.Persistence.Entities;

public sealed class TradingSystemAttachmentEntity
{
    public required string Id { get; set; }
    public required string TradingSystemId { get; set; }
    public required string FileName { get; set; }
    public required string FileExtension { get; set; }
    public required string ContentType { get; set; }
    public required long FileSizeBytes { get; set; }
    public required byte[] FileContent { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    // Navigation
    public TradingSystemEntity? TradingSystem { get; set; }
}
