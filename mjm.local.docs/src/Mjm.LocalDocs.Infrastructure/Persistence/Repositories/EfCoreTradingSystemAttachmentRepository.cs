using Microsoft.EntityFrameworkCore;
using Mjm.LocalDocs.Core.Abstractions;
using Mjm.LocalDocs.Core.Models;
using Mjm.LocalDocs.Infrastructure.Persistence.Entities;

namespace Mjm.LocalDocs.Infrastructure.Persistence.Repositories;

public sealed class EfCoreTradingSystemAttachmentRepository : ITradingSystemAttachmentRepository
{
    private readonly LocalDocsDbContext _context;

    public EfCoreTradingSystemAttachmentRepository(LocalDocsDbContext context)
    {
        _context = context;
    }

    public async Task<TradingSystemAttachment> CreateAsync(TradingSystemAttachment attachment, CancellationToken cancellationToken = default)
    {
        var entity = new TradingSystemAttachmentEntity
        {
            Id = attachment.Id,
            TradingSystemId = attachment.TradingSystemId,
            FileName = attachment.FileName,
            FileExtension = attachment.FileExtension,
            ContentType = attachment.ContentType,
            FileSizeBytes = attachment.FileSizeBytes,
            FileContent = attachment.FileContent,
            CreatedAt = attachment.CreatedAt
        };

        _context.TradingSystemAttachments.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return attachment;
    }

    public async Task<TradingSystemAttachment?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.TradingSystemAttachments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        return entity == null ? null : MapToModel(entity);
    }

    public async Task<IReadOnlyList<TradingSystemAttachment>> GetByTradingSystemIdAsync(string tradingSystemId, CancellationToken cancellationToken = default)
    {
        var entities = await _context.TradingSystemAttachments
            .AsNoTracking()
            .Where(a => a.TradingSystemId == tradingSystemId)
            .ToListAsync(cancellationToken);

        return entities.OrderByDescending(a => a.CreatedAt).Select(MapToModel).ToList();
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.TradingSystemAttachments
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (entity == null) return false;

        _context.TradingSystemAttachments.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task DeleteByTradingSystemIdAsync(string tradingSystemId, CancellationToken cancellationToken = default)
    {
        var entities = await _context.TradingSystemAttachments
            .Where(a => a.TradingSystemId == tradingSystemId)
            .ToListAsync(cancellationToken);

        _context.TradingSystemAttachments.RemoveRange(entities);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static TradingSystemAttachment MapToModel(TradingSystemAttachmentEntity entity) => new()
    {
        Id = entity.Id,
        TradingSystemId = entity.TradingSystemId,
        FileName = entity.FileName,
        FileExtension = entity.FileExtension,
        ContentType = entity.ContentType,
        FileSizeBytes = entity.FileSizeBytes,
        FileContent = entity.FileContent,
        CreatedAt = entity.CreatedAt
    };
}
