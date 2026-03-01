using Mjm.LocalDocs.Core.Models;

namespace Mjm.LocalDocs.Core.Abstractions;

/// <summary>
/// Repository for managing trading systems.
/// </summary>
public interface ITradingSystemRepository
{
    /// <summary>
    /// Creates a new trading system.
    /// </summary>
    /// <param name="tradingSystem">The trading system to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created trading system.</returns>
    Task<TradingSystem> CreateAsync(
        TradingSystem tradingSystem,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a trading system by its identifier.
    /// </summary>
    /// <param name="id">The trading system identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The trading system, or null if not found.</returns>
    Task<TradingSystem?> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all trading systems.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of all trading systems.</returns>
    Task<IReadOnlyList<TradingSystem>> GetAllAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets trading systems filtered by status.
    /// </summary>
    /// <param name="status">The status to filter by.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of trading systems with the specified status.</returns>
    Task<IReadOnlyList<TradingSystem>> GetByStatusAsync(
        TradingSystemStatus status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets trading systems for a specific project.
    /// </summary>
    /// <param name="projectId">The project identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of trading systems in the project.</returns>
    Task<IReadOnlyList<TradingSystem>> GetByProjectAsync(
        string projectId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches trading systems by name or description.
    /// </summary>
    /// <param name="searchTerm">The search term.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of matching trading systems.</returns>
    Task<IReadOnlyList<TradingSystem>> SearchAsync(
        string searchTerm,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing trading system.
    /// </summary>
    /// <param name="tradingSystem">The trading system with updated values.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated trading system.</returns>
    Task<TradingSystem> UpdateAsync(
        TradingSystem tradingSystem,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates only the status of a trading system.
    /// </summary>
    /// <param name="id">The trading system identifier.</param>
    /// <param name="status">The new status.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated trading system, or null if not found.</returns>
    Task<TradingSystem?> UpdateStatusAsync(
        string id,
        TradingSystemStatus status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the code document reference.
    /// </summary>
    /// <param name="id">The trading system identifier.</param>
    /// <param name="codeDocumentId">The code document identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated trading system, or null if not found.</returns>
    Task<TradingSystem?> UpdateCodeDocumentAsync(
        string id,
        string codeDocumentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds an attachment document to a trading system.
    /// </summary>
    /// <param name="id">The trading system identifier.</param>
    /// <param name="attachmentDocumentId">The attachment document identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated trading system, or null if not found.</returns>
    Task<TradingSystem?> AddAttachmentAsync(
        string id,
        string attachmentDocumentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes an attachment document from a trading system.
    /// </summary>
    /// <param name="id">The trading system identifier.</param>
    /// <param name="attachmentDocumentId">The attachment document identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated trading system, or null if not found.</returns>
    Task<TradingSystem?> RemoveAttachmentAsync(
        string id,
        string attachmentDocumentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a trading system.
    /// </summary>
    /// <param name="id">The trading system identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted, false if not found.</returns>
    Task<bool> DeleteAsync(
        string id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a trading system exists.
    /// </summary>
    /// <param name="id">The trading system identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if exists, false otherwise.</returns>
    Task<bool> ExistsAsync(
        string id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a trading system with the given name exists.
    /// </summary>
    /// <param name="name">The trading system name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if exists, false otherwise.</returns>
    Task<bool> ExistsByNameAsync(
        string name,
        CancellationToken cancellationToken = default);
}
