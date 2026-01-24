using System.ComponentModel;
using ModelContextProtocol.Server;
using Mjm.LocalDocs.Core.Models;
using Mjm.LocalDocs.Core.Services;

namespace Mjm.LocalDocs.Server.McpTools;

/// <summary>
/// MCP Tool for adding documents to the store.
/// </summary>
[McpServerToolType]
public sealed class AddDocumentTool
{
    private readonly DocumentService _documentService;

    public AddDocumentTool(DocumentService documentService)
    {
        _documentService = documentService;
    }

    [McpServerTool(Name = "add_document")]
    [Description("Add a new document to the documentation store. The document will be chunked and indexed for semantic search.")]
    public async Task<string> AddDocumentAsync(
        [Description("Unique identifier for the document")] string id,
        [Description("Title or name of the document")] string title,
        [Description("Full text content of the document")] string content,
        [Description("Collection name to organize documents (e.g., 'api-docs', 'tutorials', 'reference')")] string collection,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            return "Error: Document ID is required.";
        
        if (string.IsNullOrWhiteSpace(content))
            return "Error: Document content is required.";
        
        if (string.IsNullOrWhiteSpace(collection))
            return "Error: Collection name is required.";

        var document = new Document
        {
            Id = id,
            Title = title ?? id,
            Content = content,
            Collection = collection
        };

        try
        {
            await _documentService.AddDocumentAsync(document, cancellationToken);
            return $"Document '{title}' (ID: {id}) successfully added to collection '{collection}'.";
        }
        catch (Exception ex)
        {
            return $"Error adding document: {ex.Message}";
        }
    }
}
