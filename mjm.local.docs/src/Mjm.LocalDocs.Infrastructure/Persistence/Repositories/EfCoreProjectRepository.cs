using Microsoft.EntityFrameworkCore;
using Mjm.LocalDocs.Core.Abstractions;
using Mjm.LocalDocs.Core.Models;
using Mjm.LocalDocs.Infrastructure.Persistence.Entities;

namespace Mjm.LocalDocs.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of project repository.
/// Supports SQLite, SQL Server, and other EF Core providers.
/// </summary>
public sealed class EfCoreProjectRepository : IProjectRepository
{
    private readonly LocalDocsDbContext _context;

    public EfCoreProjectRepository(LocalDocsDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Project> CreateAsync(
        Project project,
        CancellationToken cancellationToken = default)
    {
        var entity = new ProjectEntity
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt
        };

        _context.Projects.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return project;
    }

    /// <inheritdoc />
    public async Task<Project?> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        return entity == null ? null : MapToModel(entity);
    }

    /// <inheritdoc />
    public async Task<Project?> GetByNameAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Name == name, cancellationToken);

        return entity == null ? null : MapToModel(entity);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Project>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.Projects
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        return entities.Select(MapToModel).ToList();
    }

    /// <inheritdoc />
    public async Task<Project> UpdateAsync(
        Project project,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == project.Id, cancellationToken);

        if (entity == null)
        {
            throw new InvalidOperationException($"Project with ID '{project.Id}' not found.");
        }

        entity.Name = project.Name;
        entity.Description = project.Description;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return MapToModel(entity);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (entity == null)
        {
            return false;
        }

        _context.Projects.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Projects
            .AnyAsync(p => p.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsByNameAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        return await _context.Projects
            .AnyAsync(p => p.Name == name, cancellationToken);
    }

    private static Project MapToModel(ProjectEntity entity)
    {
        return new Project
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }
}
