namespace Mjm.LocalDocs.Core.Models;

/// <summary>
/// Configuration options for the server hosting.
/// </summary>
public sealed class ServerOptions
{
    /// <summary>
    /// Configuration section name in appsettings.
    /// </summary>
    public const string SectionName = "LocalDocs:Server";

    /// <summary>
    /// Whether to use HTTPS for the server.
    /// When true, HTTPS redirect is enabled for the web UI (but not for MCP endpoint).
    /// When false, the server operates in HTTP-only mode with no HTTPS redirect.
    /// Default is false for maximum compatibility with MCP clients.
    /// </summary>
    /// <remarks>
    /// In production behind a reverse proxy (IIS, nginx, Azure App Service),
    /// this should typically be false as TLS termination is handled by the proxy.
    /// Set to true only when Kestrel needs to serve HTTPS directly (e.g., local development).
    /// </remarks>
    public bool UseHttps { get; set; } = false;
}
