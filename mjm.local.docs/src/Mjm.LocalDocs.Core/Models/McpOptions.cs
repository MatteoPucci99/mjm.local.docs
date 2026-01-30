namespace Mjm.LocalDocs.Core.Models;

/// <summary>
/// Configuration options for the MCP server.
/// </summary>
public sealed class McpOptions
{
    /// <summary>
    /// Configuration section name in appsettings.
    /// </summary>
    public const string SectionName = "LocalDocs:Mcp";

    /// <summary>
    /// Whether authentication is required for MCP requests.
    /// When false, the MCP endpoint is accessible without a token.
    /// Default is true.
    /// </summary>
    public bool RequireAuthentication { get; set; } = true;
}
