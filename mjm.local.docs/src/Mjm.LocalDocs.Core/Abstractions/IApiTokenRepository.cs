using Mjm.LocalDocs.Core.Models;

namespace Mjm.LocalDocs.Core.Abstractions;

/// <summary>
/// Repository for managing API tokens.
/// </summary>
public interface IApiTokenRepository
{
    /// <summary>
    /// Creates a new API token.
    /// </summary>
    /// <param name="token">The token to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created token.</returns>
    Task<ApiToken> CreateAsync(
        ApiToken token,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a token by its identifier.
    /// </summary>
    /// <param name="id">The token identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The token, or null if not found.</returns>
    Task<ApiToken?> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a token by its hash value.
    /// </summary>
    /// <param name="tokenHash">The SHA256 hash of the token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The token, or null if not found.</returns>
    Task<ApiToken?> GetByHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all tokens.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of all tokens.</returns>
    Task<IReadOnlyList<ApiToken>> GetAllAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing token.
    /// </summary>
    /// <param name="token">The token with updated values.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated token.</returns>
    Task<ApiToken> UpdateAsync(
        ApiToken token,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a token.
    /// </summary>
    /// <param name="id">The token identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted, false if not found.</returns>
    Task<bool> DeleteAsync(
        string id,
        CancellationToken cancellationToken = default);
}
