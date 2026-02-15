using System.ComponentModel;
using ModelContextProtocol.Server;
using Mjm.LocalDocs.Core.Services;

namespace Mjm.LocalDocs.Server.McpTools;

/// <summary>
/// MCP Tools for managing documents.
/// </summary>
[McpServerToolType]
public sealed class DocumentTools
{
    private readonly DocumentService _documentService;

    public DocumentTools(DocumentService documentService)
    {
        _documentService = documentService;
    }

    [McpServerTool(Name = "get_document")]
    [Description("Get details about a specific document.")]
    public async Task<string> GetDocumentAsync(
        [Description("The document ID")] string documentId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(documentId))
            return "Error: Document ID is required.";

        var document = await _documentService.GetDocumentAsync(documentId, cancellationToken);
        if (document == null)
        {
            return $"Error: Document with ID '{documentId}' not found.";
        }

        var response = $"## Document: {document.FileName}\n\n";
        response += $"**ID**: {document.Id}\n";
        response += $"**Project ID**: {document.ProjectId}\n";
        response += $"**File Extension**: {document.FileExtension}\n";
        response += $"**File Size**: {document.FileSizeBytes} bytes\n";
        response += $"**Version**: {document.VersionNumber}\n";
        if (document.IsSuperseded)
        {
            response += "**Status**: Superseded (replaced by a newer version)\n";
        }
        if (!string.IsNullOrEmpty(document.ParentDocumentId))
        {
            response += $"**Previous Version ID**: {document.ParentDocumentId}\n";
        }
        response += $"**Created**: {document.CreatedAt:yyyy-MM-dd HH:mm:ss}\n";
        if (document.UpdatedAt.HasValue)
        {
            response += $"**Updated**: {document.UpdatedAt:yyyy-MM-dd HH:mm:ss}\n";
        }
        if (!string.IsNullOrEmpty(document.ContentHash))
        {
            response += $"**Content Hash**: {document.ContentHash}\n";
        }
        response += $"\n### Extracted Text Preview (first 500 chars):\n";
        response += document.ExtractedText.Length > 500
            ? document.ExtractedText[..500] + "..."
            : document.ExtractedText;

        return response;
    }

    [McpServerTool(Name = "delete_document")]
    [Description("Delete a document and all its chunks and embeddings. This action cannot be undone!")]
    public async Task<string> DeleteDocumentAsync(
        [Description("The document ID to delete")] string documentId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(documentId))
            return "Error: Document ID is required.";

        var document = await _documentService.GetDocumentAsync(documentId, cancellationToken);
        if (document == null)
        {
            return $"Error: Document with ID '{documentId}' not found.";
        }

        try
        {
            var deleted = await _documentService.DeleteDocumentAsync(documentId, cancellationToken);
            if (deleted)
            {
                return $"Document '{document.FileName}' (ID: {documentId}) has been deleted.";
            }
            return $"Error: Failed to delete document '{documentId}'.";
        }
        catch (Exception ex)
        {
            return $"Error deleting document: {ex.Message}";
        }
    }

    [McpServerTool(Name = "list_documents")]
    [Description("List all documents in a project. By default, superseded (old) versions are hidden.")]
    public async Task<string> ListDocumentsAsync(
        [Description("The project ID to list documents for")] string projectId,
        [Description("Include superseded (old) document versions (default: false)")] bool includeSuperseded = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(projectId))
            return "Error: Project ID is required.";

        var documents = await _documentService.GetDocumentsByProjectAsync(
            projectId, includeSuperseded: includeSuperseded, cancellationToken: cancellationToken);

        if (documents.Count == 0)
        {
            return includeSuperseded
                ? $"No documents found in project '{projectId}'."
                : $"No active documents found in project '{projectId}'. Try with includeSuperseded=true to see all versions.";
        }

        var response = $"Documents in project ({documents.Count}):\n\n";

        foreach (var doc in documents)
        {
            var versionInfo = $"v{doc.VersionNumber}";
            if (doc.IsSuperseded)
                versionInfo += " [SUPERSEDED]";

            response += $"- **{doc.FileName}** ({versionInfo})\n";
            response += $"  ID: {doc.Id}\n";
            response += $"  Size: {doc.FileSizeBytes} bytes\n";
            response += $"  Created: {doc.CreatedAt:yyyy-MM-dd HH:mm:ss}\n\n";
        }

        return response;
    }

    [McpServerTool(Name = "get_document_content")]
    [Description("Get the full extracted text content of a document.")]
    public async Task<string> GetDocumentContentAsync(
        [Description("The document ID")] string documentId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(documentId))
            return "Error: Document ID is required.";

        var document = await _documentService.GetDocumentAsync(documentId, cancellationToken);
        if (document == null)
        {
            return $"Error: Document with ID '{documentId}' not found.";
        }

        return $"# {document.FileName}\n\n{document.ExtractedText}";
    }
}
