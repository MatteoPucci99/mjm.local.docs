namespace Mjm.LocalDocs.Server.Dtos;

public record CreateTradingSystemRequest(
    string Name,
    string? Description,
    string? SourceUrl,
    IReadOnlyList<string>? Tags,
    string? Notes
);
