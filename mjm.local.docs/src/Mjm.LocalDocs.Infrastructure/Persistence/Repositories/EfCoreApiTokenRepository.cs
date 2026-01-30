using Microsoft.EntityFrameworkCore;
using Mjm.LocalDocs.Core.Abstractions;
using Mjm.LocalDocs.Core.Models;
using Mjm.LocalDocs.Infrastructure.Persistence.Entities;

namespace Mjm.LocalDocs.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of API token repository.
/// Supports SQLite, SQL Server, and other EF Core providers.
/// </summary>
public sealed class EfCoreApiTokenRepository : IApiTokenRepository
{
    private readonly LocalDocsDbContext _context;

    public EfCoreApiTokenRepository(LocalDocsDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<ApiToken> CreateAsync(
        ApiToken token,
        CancellationToken cancellationToken = default)
    {
        var entity = new ApiTokenEntity
        {
            Id = token.Id,
            Name = token.Name,
            TokenHash = token.TokenHash,
            TokenPrefix = token.TokenPrefix,
            CreatedAt = token.CreatedAt,
            ExpiresAt = token.ExpiresAt,
            LastUsedAt = token.LastUsedAt,
            IsRevoked = token.IsRevoked
        };

        _context.ApiTokens.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return token;
    }

    /// <inheritdoc />
    public async Task<ApiToken?> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.ApiTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        return entity == null ? null : MapToModel(entity);
    }

    /// <inheritdoc />
    public async Task<ApiToken?> GetByHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.ApiTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        return entity == null ? null : MapToModel(entity);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ApiToken>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.ApiTokens
            .AsNoTracking()
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);

        return entities.Select(MapToModel).ToList();
    }

    /// <inheritdoc />
    public async Task<ApiToken> UpdateAsync(
        ApiToken token,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.ApiTokens
            .FirstOrDefaultAsync(t => t.Id == token.Id, cancellationToken);

        if (entity == null)
        {
            throw new InvalidOperationException($"API token with ID '{token.Id}' not found.");
        }

        entity.Name = token.Name;
        entity.LastUsedAt = token.LastUsedAt;
        entity.IsRevoked = token.IsRevoked;

        await _context.SaveChangesAsync(cancellationToken);

        return MapToModel(entity);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.ApiTokens
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (entity == null)
        {
            return false;
        }

        _context.ApiTokens.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    private static ApiToken MapToModel(ApiTokenEntity entity)
    {
        return new ApiToken
        {
            Id = entity.Id,
            Name = entity.Name,
            TokenHash = entity.TokenHash,
            TokenPrefix = entity.TokenPrefix,
            CreatedAt = entity.CreatedAt,
            ExpiresAt = entity.ExpiresAt,
            LastUsedAt = entity.LastUsedAt,
            IsRevoked = entity.IsRevoked
        };
    }
}
