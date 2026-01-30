using Mjm.LocalDocs.Core.Models;

namespace Mjm.LocalDocs.Core.Abstractions;

/// <summary>
/// Repository for managing projects.
/// </summary>
public interface IProjectRepository
{
    /// <summary>
    /// Creates a new project.
    /// </summary>
    /// <param name="project">The project to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created project.</returns>
    Task<Project> CreateAsync(
        Project project,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a project by its identifier.
    /// </summary>
    /// <param name="id">The project identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The project, or null if not found.</returns>
    Task<Project?> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a project by its name.
    /// </summary>
    /// <param name="name">The project name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The project, or null if not found.</returns>
    Task<Project?> GetByNameAsync(
        string name,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all projects.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of all projects.</returns>
    Task<IReadOnlyList<Project>> GetAllAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing project.
    /// </summary>
    /// <param name="project">The project with updated values.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated project.</returns>
    Task<Project> UpdateAsync(
        Project project,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a project and all its documents.
    /// </summary>
    /// <param name="id">The project identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted, false if not found.</returns>
    Task<bool> DeleteAsync(
        string id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a project exists.
    /// </summary>
    /// <param name="id">The project identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if exists, false otherwise.</returns>
    Task<bool> ExistsAsync(
        string id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a project with the given name exists.
    /// </summary>
    /// <param name="name">The project name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if exists, false otherwise.</returns>
    Task<bool> ExistsByNameAsync(
        string name,
        CancellationToken cancellationToken = default);
}
