namespace Mjm.LocalDocs.Core.Models;

/// <summary>
/// Represents a trading system with its metadata, code reference, and attachments.
/// </summary>
public sealed class TradingSystem
{
    /// <summary>
    /// Unique identifier for the trading system.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Name of the trading system.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Optional description of the trading system strategy.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Optional URL to the source/reference material.
    /// </summary>
    public string? SourceUrl { get; init; }

    /// <summary>
    /// Current lifecycle status of the trading system.
    /// </summary>
    public required TradingSystemStatus Status { get; init; }

    /// <summary>
    /// The project this trading system belongs to.
    /// </summary>
    public required string ProjectId { get; init; }

    /// <summary>
    /// ID of the Document containing the EasyLanguage code.
    /// Null if code has not been added yet.
    /// </summary>
    public string? CodeDocumentId { get; init; }

    /// <summary>
    /// IDs of attached Documents (backtest reports, screenshots, etc.).
    /// </summary>
    public IReadOnlyList<string> AttachmentDocumentIds { get; init; } = [];

    /// <summary>
    /// Tags for categorization (e.g., "trend-following", "mean-reversion").
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>
    /// Free-form notes about the trading system.
    /// </summary>
    public string? Notes { get; init; }

    /// <summary>
    /// Timestamp when the trading system was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Timestamp when the trading system was last updated.
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; set; }
}
