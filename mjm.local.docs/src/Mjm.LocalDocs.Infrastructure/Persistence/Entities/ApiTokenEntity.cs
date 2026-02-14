namespace Mjm.LocalDocs.Infrastructure.Persistence.Entities;

/// <summary>
/// EF Core entity for API tokens.
/// </summary>
public sealed class ApiTokenEntity
{
    /// <summary>
    /// Unique identifier for the token.
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// User-friendly name for identifying the token.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// SHA256 hash of the token value.
    /// </summary>
    public required string TokenHash { get; set; }

    /// <summary>
    /// First 8 characters of the token for identification.
    /// </summary>
    public string? TokenPrefix { get; set; }

    /// <summary>
    /// When the token was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// When the token expires. Null means never expires.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>
    /// When the token was last used.
    /// </summary>
    public DateTimeOffset? LastUsedAt { get; set; }

    /// <summary>
    /// Whether the token has been revoked.
    /// </summary>
    public bool IsRevoked { get; set; }
}
