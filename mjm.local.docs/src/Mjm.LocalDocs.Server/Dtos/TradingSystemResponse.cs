namespace Mjm.LocalDocs.Server.Dtos;

public record TradingSystemResponse(
    string Id,
    string Name,
    string? Description,
    string? SourceUrl,
    string Status,
    string ProjectId,
    string? CodeDocumentId,
    IReadOnlyList<string> AttachmentDocumentIds,
    IReadOnlyList<string> Tags,
    string? Notes,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt
);

public record TradingSystemListItemResponse(
    string Id,
    string Name,
    string? Description,
    string Status,
    IReadOnlyList<string> Tags,
    bool HasCode,
    int AttachmentCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt
);

public record TradingSystemAttachmentResponse(
    string Id,
    string TradingSystemId,
    string FileName,
    string FileExtension,
    string ContentType,
    long FileSizeBytes,
    DateTimeOffset CreatedAt
);
