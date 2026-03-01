using System.Collections.Concurrent;
using Mjm.LocalDocs.Core.Abstractions;
using Mjm.LocalDocs.Core.Models;

namespace Mjm.LocalDocs.Infrastructure.VectorStore;

/// <summary>
/// In-memory implementation of ITradingSystemAttachmentRepository for development/testing.
/// </summary>
public sealed class InMemoryTradingSystemAttachmentRepository : ITradingSystemAttachmentRepository
{
    private readonly ConcurrentDictionary<string, TradingSystemAttachment> _attachments = new();

    public Task<TradingSystemAttachment> CreateAsync(TradingSystemAttachment attachment, CancellationToken cancellationToken = default)
    {
        _attachments[attachment.Id] = attachment;
        return Task.FromResult(attachment);
    }

    public Task<TradingSystemAttachment?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        _attachments.TryGetValue(id, out var attachment);
        return Task.FromResult(attachment);
    }

    public Task<IReadOnlyList<TradingSystemAttachment>> GetByTradingSystemIdAsync(string tradingSystemId, CancellationToken cancellationToken = default)
    {
        var attachments = _attachments.Values
            .Where(a => a.TradingSystemId == tradingSystemId)
            .OrderByDescending(a => a.CreatedAt)
            .ToList();
        return Task.FromResult<IReadOnlyList<TradingSystemAttachment>>(attachments);
    }

    public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_attachments.TryRemove(id, out _));
    }

    public Task DeleteByTradingSystemIdAsync(string tradingSystemId, CancellationToken cancellationToken = default)
    {
        var toRemove = _attachments.Values
            .Where(a => a.TradingSystemId == tradingSystemId)
            .Select(a => a.Id)
            .ToList();

        foreach (var id in toRemove)
            _attachments.TryRemove(id, out _);

        return Task.CompletedTask;
    }
}
