using Mjm.LocalDocs.Core.Models;

namespace Mjm.LocalDocs.Core.Abstractions;

public interface ITradingSystemAttachmentRepository
{
    Task<TradingSystemAttachment> CreateAsync(TradingSystemAttachment attachment, CancellationToken cancellationToken = default);
    Task<TradingSystemAttachment?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TradingSystemAttachment>> GetByTradingSystemIdAsync(string tradingSystemId, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
    Task DeleteByTradingSystemIdAsync(string tradingSystemId, CancellationToken cancellationToken = default);
}
