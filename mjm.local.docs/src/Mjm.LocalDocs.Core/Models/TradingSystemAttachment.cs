namespace Mjm.LocalDocs.Core.Models;

public sealed class TradingSystemAttachment
{
    public required string Id { get; init; }
    public required string TradingSystemId { get; init; }
    public required string FileName { get; init; }
    public required string FileExtension { get; init; }
    public required string ContentType { get; init; }
    public required long FileSizeBytes { get; init; }
    public required byte[] FileContent { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
