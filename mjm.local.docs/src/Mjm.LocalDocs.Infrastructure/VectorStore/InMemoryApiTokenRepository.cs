using System.Collections.Concurrent;
using Mjm.LocalDocs.Core.Abstractions;
using Mjm.LocalDocs.Core.Models;

namespace Mjm.LocalDocs.Infrastructure.VectorStore;

/// <summary>
/// In-memory implementation of API token repository for development/testing.
/// </summary>
public sealed class InMemoryApiTokenRepository : IApiTokenRepository
{
    private readonly ConcurrentDictionary<string, ApiToken> _tokens = new();

    /// <inheritdoc />
    public Task<ApiToken> CreateAsync(
        ApiToken token,
        CancellationToken cancellationToken = default)
    {
        _tokens[token.Id] = token;
        return Task.FromResult(token);
    }

    /// <inheritdoc />
    public Task<ApiToken?> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        _tokens.TryGetValue(id, out var token);
        return Task.FromResult(token);
    }

    /// <inheritdoc />
    public Task<ApiToken?> GetByHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        var token = _tokens.Values
            .FirstOrDefault(t => t.TokenHash == tokenHash);
        return Task.FromResult(token);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ApiToken>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var tokens = _tokens.Values
            .OrderByDescending(t => t.CreatedAt)
            .ToList();
        return Task.FromResult<IReadOnlyList<ApiToken>>(tokens);
    }

    /// <inheritdoc />
    public Task<ApiToken> UpdateAsync(
        ApiToken token,
        CancellationToken cancellationToken = default)
    {
        _tokens[token.Id] = token;
        return Task.FromResult(token);
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_tokens.TryRemove(id, out _));
    }
}
