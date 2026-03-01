namespace Mjm.LocalDocs.Server.Dtos;

public record UpdateTradingSystemRequest(
    string Name,
    string? Description,
    string? SourceUrl,
    IReadOnlyList<string>? Tags,
    string? Notes
);

public record UpdateTradingSystemStatusRequest(
    string Status
);

public record SaveTradingSystemCodeRequest(
    string Code
);
