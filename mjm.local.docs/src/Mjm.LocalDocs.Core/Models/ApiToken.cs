namespace Mjm.LocalDocs.Core.Models;

/// <summary>
/// Represents an API token for MCP authentication.
/// </summary>
public sealed class ApiToken
{
    /// <summary>
    /// Unique identifier for the token.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// User-friendly name for identifying the token.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// SHA256 hash of the token value. The plain text token is never stored.
    /// </summary>
    public required string TokenHash { get; init; }

    /// <summary>
    /// First 8 characters of the token for identification purposes.
    /// </summary>
    public string? TokenPrefix { get; init; }

    /// <summary>
    /// When the token was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// When the token expires. Null means the token never expires.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>
    /// When the token was last used for authentication.
    /// </summary>
    public DateTimeOffset? LastUsedAt { get; set; }

    /// <summary>
    /// Whether the token has been revoked.
    /// </summary>
    public bool IsRevoked { get; set; }

    /// <summary>
    /// Checks if the token is currently valid (not expired and not revoked).
    /// </summary>
    public bool IsValid => !IsRevoked && (ExpiresAt == null || ExpiresAt > DateTimeOffset.UtcNow);
}
