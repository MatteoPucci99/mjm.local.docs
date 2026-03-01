using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Mjm.LocalDocs.Core.Abstractions;
using Mjm.LocalDocs.Core.Models;
using Mjm.LocalDocs.Infrastructure.Persistence.Entities;

namespace Mjm.LocalDocs.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of ITradingSystemRepository.
/// </summary>
public sealed class EfCoreTradingSystemRepository : ITradingSystemRepository
{
    private readonly LocalDocsDbContext _context;

    public EfCoreTradingSystemRepository(LocalDocsDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<TradingSystem> CreateAsync(
        TradingSystem tradingSystem,
        CancellationToken cancellationToken = default)
    {
        var entity = MapToEntity(tradingSystem);
        _context.TradingSystems.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return MapToModel(entity);
    }

    /// <inheritdoc />
    public async Task<TradingSystem?> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.TradingSystems
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        return entity == null ? null : MapToModel(entity);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TradingSystem>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.TradingSystems
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return entities
            .OrderByDescending(t => t.UpdatedAt ?? t.CreatedAt)
            .Select(MapToModel).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TradingSystem>> GetByStatusAsync(
        TradingSystemStatus status,
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.TradingSystems
            .AsNoTracking()
            .Where(t => t.Status == status)
            .ToListAsync(cancellationToken);

        return entities
            .OrderByDescending(t => t.UpdatedAt ?? t.CreatedAt)
            .Select(MapToModel).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TradingSystem>> GetByProjectAsync(
        string projectId,
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.TradingSystems
            .AsNoTracking()
            .Where(t => t.ProjectId == projectId)
            .ToListAsync(cancellationToken);

        return entities
            .OrderByDescending(t => t.UpdatedAt ?? t.CreatedAt)
            .Select(MapToModel).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TradingSystem>> SearchAsync(
        string searchTerm,
        CancellationToken cancellationToken = default)
    {
        var lowerTerm = searchTerm.ToLowerInvariant();
        var entities = await _context.TradingSystems
            .AsNoTracking()
            .Where(t => t.Name.ToLower().Contains(lowerTerm) ||
                        (t.Description != null && t.Description.ToLower().Contains(lowerTerm)) ||
                        (t.Notes != null && t.Notes.ToLower().Contains(lowerTerm)))
            .ToListAsync(cancellationToken);

        return entities
            .OrderByDescending(t => t.UpdatedAt ?? t.CreatedAt)
            .Select(MapToModel).ToList();
    }

    /// <inheritdoc />
    public async Task<TradingSystem> UpdateAsync(
        TradingSystem tradingSystem,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.TradingSystems
            .FirstOrDefaultAsync(t => t.Id == tradingSystem.Id, cancellationToken);

        if (entity == null)
            throw new InvalidOperationException($"Trading system '{tradingSystem.Id}' not found.");

        entity.Name = tradingSystem.Name;
        entity.Description = tradingSystem.Description;
        entity.SourceUrl = tradingSystem.SourceUrl;
        entity.Status = tradingSystem.Status;
        entity.CodeDocumentId = tradingSystem.CodeDocumentId;
        entity.AttachmentDocumentIdsJson = SerializeList(tradingSystem.AttachmentDocumentIds);
        entity.TagsJson = SerializeList(tradingSystem.Tags);
        entity.Notes = tradingSystem.Notes;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return MapToModel(entity);
    }

    /// <inheritdoc />
    public async Task<TradingSystem?> UpdateStatusAsync(
        string id,
        TradingSystemStatus status,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.TradingSystems
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (entity == null)
            return null;

        entity.Status = status;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return MapToModel(entity);
    }

    /// <inheritdoc />
    public async Task<TradingSystem?> UpdateCodeDocumentAsync(
        string id,
        string codeDocumentId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.TradingSystems
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (entity == null)
            return null;

        entity.CodeDocumentId = codeDocumentId;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return MapToModel(entity);
    }

    /// <inheritdoc />
    public async Task<TradingSystem?> AddAttachmentAsync(
        string id,
        string attachmentDocumentId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.TradingSystems
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (entity == null)
            return null;

        var attachments = DeserializeList(entity.AttachmentDocumentIdsJson).ToList();
        if (!attachments.Contains(attachmentDocumentId))
        {
            attachments.Add(attachmentDocumentId);
            entity.AttachmentDocumentIdsJson = SerializeList(attachments);
            entity.UpdatedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }

        return MapToModel(entity);
    }

    /// <inheritdoc />
    public async Task<TradingSystem?> RemoveAttachmentAsync(
        string id,
        string attachmentDocumentId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.TradingSystems
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (entity == null)
            return null;

        var attachments = DeserializeList(entity.AttachmentDocumentIdsJson).ToList();
        if (attachments.Remove(attachmentDocumentId))
        {
            entity.AttachmentDocumentIdsJson = SerializeList(attachments);
            entity.UpdatedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }

        return MapToModel(entity);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.TradingSystems
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (entity == null)
            return false;

        _context.TradingSystems.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        return await _context.TradingSystems
            .AnyAsync(t => t.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsByNameAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        return await _context.TradingSystems
            .AnyAsync(t => t.Name == name, cancellationToken);
    }

    private static TradingSystemEntity MapToEntity(TradingSystem model)
    {
        return new TradingSystemEntity
        {
            Id = model.Id,
            Name = model.Name,
            Description = model.Description,
            SourceUrl = model.SourceUrl,
            Status = model.Status,
            ProjectId = model.ProjectId,
            CodeDocumentId = model.CodeDocumentId,
            AttachmentDocumentIdsJson = SerializeList(model.AttachmentDocumentIds),
            TagsJson = SerializeList(model.Tags),
            Notes = model.Notes,
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt
        };
    }

    private static TradingSystem MapToModel(TradingSystemEntity entity)
    {
        return new TradingSystem
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            SourceUrl = entity.SourceUrl,
            Status = entity.Status,
            ProjectId = entity.ProjectId,
            CodeDocumentId = entity.CodeDocumentId,
            AttachmentDocumentIds = DeserializeList(entity.AttachmentDocumentIdsJson),
            Tags = DeserializeList(entity.TagsJson),
            Notes = entity.Notes,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    private static string? SerializeList(IReadOnlyList<string> list)
    {
        if (list.Count == 0)
            return null;
        return JsonSerializer.Serialize(list);
    }

    private static IReadOnlyList<string> DeserializeList(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return [];
        return JsonSerializer.Deserialize<List<string>>(json) ?? [];
    }
}
