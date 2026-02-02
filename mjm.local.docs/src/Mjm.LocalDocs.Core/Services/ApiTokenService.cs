using System.Security.Cryptography;
using Mjm.LocalDocs.Core.Abstractions;
using Mjm.LocalDocs.Core.Models;

namespace Mjm.LocalDocs.Core.Services;

/// <summary>
/// Service for managing API tokens for MCP authentication.
/// </summary>
public sealed class ApiTokenService
{
    private readonly IApiTokenRepository _repository;

    /// <summary>
    /// Creates a new ApiTokenService.
    /// </summary>
    /// <param name="repository">Token repository.</param>
    public ApiTokenService(IApiTokenRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Creates a new API token.
    /// </summary>
    /// <param name="name">User-friendly name for the token.</param>
    /// <param name="expiresAt">Optional expiration date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple containing the created token metadata and the plain text token value.
    /// The plain text token is only returned once and should be shown to the user immediately.</returns>
    public async Task<(ApiToken Token, string PlainTextToken)> CreateTokenAsync(
        string name,
        DateTimeOffset? expiresAt = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Token name is required.", nameof(name));
        }

        // Generate a secure random token (32 bytes = 256 bits)
        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var plainTextToken = Convert.ToBase64String(tokenBytes);

        // Hash the token for storage
        var tokenHash = ComputeHash(plainTextToken);

        // Extract prefix for identification (first 8 chars)
        var tokenPrefix = plainTextToken[..Math.Min(8, plainTextToken.Length)];

        var token = new ApiToken
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = name,
            TokenHash = tokenHash,
            TokenPrefix = tokenPrefix,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = expiresAt,
            LastUsedAt = null,
            IsRevoked = false
        };

        var createdToken = await _repository.CreateAsync(token, cancellationToken);

        return (createdToken, plainTextToken);
    }

    /// <summary>
    /// Validates a token and optionally updates its last used timestamp.
    /// </summary>
    /// <param name="plainTextToken">The plain text token value.</param>
    /// <param name="updateLastUsed">Whether to update the LastUsedAt timestamp.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The validated token if valid, null otherwise.</returns>
    public async Task<ApiToken?> ValidateTokenAsync(
        string plainTextToken,
        bool updateLastUsed = true,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(plainTextToken))
        {
            return null;
        }

        var tokenHash = ComputeHash(plainTextToken);
        var token = await _repository.GetByHashAsync(tokenHash, cancellationToken);

        if (token == null)
        {
            return null;
        }

        // Check if token is valid (not revoked and not expired)
        if (!token.IsValid)
        {
            return null;
        }

        // Update last used timestamp
        if (updateLastUsed)
        {
            token.LastUsedAt = DateTimeOffset.UtcNow;
            await _repository.UpdateAsync(token, cancellationToken);
        }

        return token;
    }

    /// <summary>
    /// Gets all tokens.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of all tokens (without hash values exposed).</returns>
    public Task<IReadOnlyList<ApiToken>> GetAllTokensAsync(
        CancellationToken cancellationToken = default)
    {
        return _repository.GetAllAsync(cancellationToken);
    }

    /// <summary>
    /// Gets a token by its identifier.
    /// </summary>
    /// <param name="id">The token identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The token, or null if not found.</returns>
    public Task<ApiToken?> GetTokenByIdAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        return _repository.GetByIdAsync(id, cancellationToken);
    }

    /// <summary>
    /// Revokes a token, making it invalid for future authentication.
    /// </summary>
    /// <param name="id">The token identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the token was revoked, false if not found.</returns>
    public async Task<bool> RevokeTokenAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        var token = await _repository.GetByIdAsync(id, cancellationToken);
        if (token == null)
        {
            return false;
        }

        token.IsRevoked = true;
        await _repository.UpdateAsync(token, cancellationToken);
        return true;
    }

    /// <summary>
    /// Permanently deletes a token.
    /// </summary>
    /// <param name="id">The token identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted, false if not found.</returns>
    public Task<bool> DeleteTokenAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        return _repository.DeleteAsync(id, cancellationToken);
    }

    /// <summary>
    /// Computes SHA256 hash of the token.
    /// </summary>
    /// <param name="plainTextToken">The plain text token.</param>
    /// <returns>Base64-encoded hash.</returns>
    private static string ComputeHash(string plainTextToken)
    {
        var tokenBytes = System.Text.Encoding.UTF8.GetBytes(plainTextToken);
        var hashBytes = SHA256.HashData(tokenBytes);
        return Convert.ToBase64String(hashBytes);
    }
}
