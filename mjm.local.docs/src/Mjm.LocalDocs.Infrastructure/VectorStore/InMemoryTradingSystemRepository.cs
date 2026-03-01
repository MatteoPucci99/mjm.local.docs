using System.Collections.Concurrent;
using Mjm.LocalDocs.Core.Abstractions;
using Mjm.LocalDocs.Core.Models;

namespace Mjm.LocalDocs.Infrastructure.VectorStore;

/// <summary>
/// In-memory implementation of ITradingSystemRepository for development/testing.
/// </summary>
public sealed class InMemoryTradingSystemRepository : ITradingSystemRepository
{
    private readonly ConcurrentDictionary<string, TradingSystem> _systems = new();

    /// <inheritdoc />
    public Task<TradingSystem> CreateAsync(
        TradingSystem tradingSystem,
        CancellationToken cancellationToken = default)
    {
        _systems[tradingSystem.Id] = tradingSystem;
        return Task.FromResult(tradingSystem);
    }

    /// <inheritdoc />
    public Task<TradingSystem?> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        _systems.TryGetValue(id, out var system);
        return Task.FromResult(system);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<TradingSystem>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var systems = _systems.Values
            .OrderByDescending(t => t.UpdatedAt ?? t.CreatedAt)
            .ToList();
        return Task.FromResult<IReadOnlyList<TradingSystem>>(systems);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<TradingSystem>> GetByStatusAsync(
        TradingSystemStatus status,
        CancellationToken cancellationToken = default)
    {
        var systems = _systems.Values
            .Where(t => t.Status == status)
            .OrderByDescending(t => t.UpdatedAt ?? t.CreatedAt)
            .ToList();
        return Task.FromResult<IReadOnlyList<TradingSystem>>(systems);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<TradingSystem>> GetByProjectAsync(
        string projectId,
        CancellationToken cancellationToken = default)
    {
        var systems = _systems.Values
            .Where(t => t.ProjectId == projectId)
            .OrderByDescending(t => t.UpdatedAt ?? t.CreatedAt)
            .ToList();
        return Task.FromResult<IReadOnlyList<TradingSystem>>(systems);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<TradingSystem>> SearchAsync(
        string searchTerm,
        CancellationToken cancellationToken = default)
    {
        var lowerTerm = searchTerm.ToLowerInvariant();
        var systems = _systems.Values
            .Where(t => t.Name.ToLowerInvariant().Contains(lowerTerm) ||
                        (t.Description?.ToLowerInvariant().Contains(lowerTerm) ?? false) ||
                        (t.Notes?.ToLowerInvariant().Contains(lowerTerm) ?? false))
            .OrderByDescending(t => t.UpdatedAt ?? t.CreatedAt)
            .ToList();
        return Task.FromResult<IReadOnlyList<TradingSystem>>(systems);
    }

    /// <inheritdoc />
    public Task<TradingSystem> UpdateAsync(
        TradingSystem tradingSystem,
        CancellationToken cancellationToken = default)
    {
        _systems[tradingSystem.Id] = tradingSystem;
        return Task.FromResult(tradingSystem);
    }

    /// <inheritdoc />
    public Task<TradingSystem?> UpdateStatusAsync(
        string id,
        TradingSystemStatus status,
        CancellationToken cancellationToken = default)
    {
        if (!_systems.TryGetValue(id, out var existing))
            return Task.FromResult<TradingSystem?>(null);

        var updated = new TradingSystem
        {
            Id = existing.Id,
            Name = existing.Name,
            Description = existing.Description,
            SourceUrl = existing.SourceUrl,
            Status = status,
            ProjectId = existing.ProjectId,
            CodeDocumentId = existing.CodeDocumentId,
            AttachmentDocumentIds = existing.AttachmentDocumentIds,
            Tags = existing.Tags,
            Notes = existing.Notes,
            CreatedAt = existing.CreatedAt,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _systems[id] = updated;
        return Task.FromResult<TradingSystem?>(updated);
    }

    /// <inheritdoc />
    public Task<TradingSystem?> UpdateCodeDocumentAsync(
        string id,
        string codeDocumentId,
        CancellationToken cancellationToken = default)
    {
        if (!_systems.TryGetValue(id, out var existing))
            return Task.FromResult<TradingSystem?>(null);

        var updated = new TradingSystem
        {
            Id = existing.Id,
            Name = existing.Name,
            Description = existing.Description,
            SourceUrl = existing.SourceUrl,
            Status = existing.Status,
            ProjectId = existing.ProjectId,
            CodeDocumentId = codeDocumentId,
            AttachmentDocumentIds = existing.AttachmentDocumentIds,
            Tags = existing.Tags,
            Notes = existing.Notes,
            CreatedAt = existing.CreatedAt,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _systems[id] = updated;
        return Task.FromResult<TradingSystem?>(updated);
    }

    /// <inheritdoc />
    public Task<TradingSystem?> AddAttachmentAsync(
        string id,
        string attachmentDocumentId,
        CancellationToken cancellationToken = default)
    {
        if (!_systems.TryGetValue(id, out var existing))
            return Task.FromResult<TradingSystem?>(null);

        var attachments = existing.AttachmentDocumentIds.ToList();
        if (!attachments.Contains(attachmentDocumentId))
            attachments.Add(attachmentDocumentId);

        var updated = new TradingSystem
        {
            Id = existing.Id,
            Name = existing.Name,
            Description = existing.Description,
            SourceUrl = existing.SourceUrl,
            Status = existing.Status,
            ProjectId = existing.ProjectId,
            CodeDocumentId = existing.CodeDocumentId,
            AttachmentDocumentIds = attachments,
            Tags = existing.Tags,
            Notes = existing.Notes,
            CreatedAt = existing.CreatedAt,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _systems[id] = updated;
        return Task.FromResult<TradingSystem?>(updated);
    }

    /// <inheritdoc />
    public Task<TradingSystem?> RemoveAttachmentAsync(
        string id,
        string attachmentDocumentId,
        CancellationToken cancellationToken = default)
    {
        if (!_systems.TryGetValue(id, out var existing))
            return Task.FromResult<TradingSystem?>(null);

        var attachments = existing.AttachmentDocumentIds.ToList();
        attachments.Remove(attachmentDocumentId);

        var updated = new TradingSystem
        {
            Id = existing.Id,
            Name = existing.Name,
            Description = existing.Description,
            SourceUrl = existing.SourceUrl,
            Status = existing.Status,
            ProjectId = existing.ProjectId,
            CodeDocumentId = existing.CodeDocumentId,
            AttachmentDocumentIds = attachments,
            Tags = existing.Tags,
            Notes = existing.Notes,
            CreatedAt = existing.CreatedAt,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _systems[id] = updated;
        return Task.FromResult<TradingSystem?>(updated);
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_systems.TryRemove(id, out _));
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_systems.ContainsKey(id));
    }

    /// <inheritdoc />
    public Task<bool> ExistsByNameAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_systems.Values.Any(t => t.Name == name));
    }
}
