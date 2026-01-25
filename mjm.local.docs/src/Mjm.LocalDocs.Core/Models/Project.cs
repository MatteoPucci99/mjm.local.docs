namespace Mjm.LocalDocs.Core.Models;

/// <summary>
/// Represents a project that groups related documents.
/// </summary>
public sealed class Project
{
    /// <summary>
    /// Unique identifier for the project.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Unique name of the project.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Optional description of the project.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Timestamp when the project was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Timestamp when the project was last updated.
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; set; }
}
