using System.ComponentModel;
using ModelContextProtocol.Server;
using Mjm.LocalDocs.Core.Services;

namespace Mjm.LocalDocs.Server.McpTools;

/// <summary>
/// MCP Tool for listing available collections.
/// </summary>
[McpServerToolType]
public sealed class ListCollectionsTool
{
    private readonly DocumentService _documentService;

    public ListCollectionsTool(DocumentService documentService)
    {
        _documentService = documentService;
    }

    [McpServerTool(Name = "list_collections")]
    [Description("List all available document collections in the store.")]
    public async Task<string> ListCollectionsAsync(CancellationToken cancellationToken = default)
    {
        var collections = await _documentService.GetCollectionsAsync(cancellationToken);

        if (collections.Count == 0)
        {
            return "No collections found. Add documents using the add_document tool to create collections.";
        }

        var response = $"Available collections ({collections.Count}):\n\n";
        
        foreach (var collection in collections)
        {
            response += $"- {collection}\n";
        }

        return response;
    }
}
