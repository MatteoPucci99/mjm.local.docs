using System.Collections.Concurrent;
using Mjm.LocalDocs.Core.Abstractions;
using Mjm.LocalDocs.Core.Models;

namespace Mjm.LocalDocs.Infrastructure.VectorStore;

/// <summary>
/// In-memory implementation of project repository for development/testing.
/// </summary>
public sealed class InMemoryProjectRepository : IProjectRepository
{
    private readonly ConcurrentDictionary<string, Project> _projects = new();

    /// <inheritdoc />
    public Task<Project> CreateAsync(
        Project project,
        CancellationToken cancellationToken = default)
    {
        _projects[project.Id] = project;
        return Task.FromResult(project);
    }

    /// <inheritdoc />
    public Task<Project?> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        _projects.TryGetValue(id, out var project);
        return Task.FromResult(project);
    }

    /// <inheritdoc />
    public Task<Project?> GetByNameAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        var project = _projects.Values
            .FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(project);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Project>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var projects = _projects.Values
            .OrderBy(p => p.Name)
            .ToList();
        return Task.FromResult<IReadOnlyList<Project>>(projects);
    }

    /// <inheritdoc />
    public Task<Project> UpdateAsync(
        Project project,
        CancellationToken cancellationToken = default)
    {
        var updatedProject = new Project
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            CreatedAt = project.CreatedAt,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        _projects[project.Id] = updatedProject;
        return Task.FromResult(updatedProject);
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_projects.TryRemove(id, out _));
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_projects.ContainsKey(id));
    }

    /// <inheritdoc />
    public Task<bool> ExistsByNameAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        var exists = _projects.Values
            .Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(exists);
    }
}
