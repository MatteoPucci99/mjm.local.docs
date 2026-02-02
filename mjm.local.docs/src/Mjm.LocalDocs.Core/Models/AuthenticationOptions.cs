namespace Mjm.LocalDocs.Core.Models;

/// <summary>
/// Configuration options for simple username/password authentication.
/// </summary>
public sealed class AuthenticationOptions
{
    /// <summary>
    /// The configuration section name for authentication options.
    /// </summary>
    public const string SectionName = "LocalDocs:Authentication";

    /// <summary>
    /// The username for authentication.
    /// </summary>
    public required string Username { get; init; }

    /// <summary>
    /// The password for authentication.
    /// </summary>
    public required string Password { get; init; }
}
