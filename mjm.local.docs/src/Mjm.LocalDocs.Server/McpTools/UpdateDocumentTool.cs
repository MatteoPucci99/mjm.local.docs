using System.ComponentModel;
using System.Security.Cryptography;
using System.Text;
using ModelContextProtocol.Server;
using Mjm.LocalDocs.Core.Models;
using Mjm.LocalDocs.Core.Services;

namespace Mjm.LocalDocs.Server.McpTools;

/// <summary>
/// MCP Tool for updating documents by creating new versions.
/// </summary>
[McpServerToolType]
public sealed class UpdateDocumentTool
{
    private readonly DocumentService _documentService;

    public UpdateDocumentTool(DocumentService documentService)
    {
        _documentService = documentService;
    }

    [McpServerTool(Name = "update_document")]
    [Description("Update a document by creating a new version with updated content. " +
                 "The previous version is preserved as history but removed from search results. " +
                 "For binary formats (PDF, DOCX), the updated content is saved as Markdown (.md). " +
                 "Use this after retrieving document content with get_document_content, making changes, " +
                 "and wanting to save the updated version.")]
    public async Task<string> UpdateDocumentAsync(
        [Description("The ID of the document to update")] string documentId,
        [Description("The updated text content (plain text or Markdown)")] string content,
        [Description("Optional new file name (e.g., 'FRD_v2.0.md'). " +
                     "If omitted, the original name is reused with .md extension for binary format upgrades")]
        string? fileName = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(documentId))
            return "Error: Document ID is required.";

        if (string.IsNullOrWhiteSpace(content))
            return "Error: Updated content is required.";

        // 1. Get the existing document
        var existing = await _documentService.GetDocumentAsync(documentId, cancellationToken);
        if (existing is null)
            return $"Error: Document with ID '{documentId}' not found.";

        if (existing.IsSuperseded)
            return $"Error: Document '{documentId}' is already superseded. Update the latest version instead.";

        // 2. Determine the file name for the new version
        var newFileName = DetermineFileName(existing, fileName);
        var newExtension = Path.GetExtension(newFileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(newExtension))
        {
            newExtension = ".md";
            newFileName += newExtension;
        }

        // 3. Convert content to bytes (always text-based for LLM updates)
        var fileContent = Encoding.UTF8.GetBytes(content);
        var contentHash = ComputeHash(fileContent);

        // 4. Create the new version document
        var newDocument = new Document
        {
            Id = Guid.NewGuid().ToString(),
            ProjectId = existing.ProjectId,
            FileName = newFileName,
            FileExtension = newExtension,
            FileContent = fileContent,
            FileSizeBytes = fileContent.Length,
            ExtractedText = content,
            ContentHash = contentHash,
            VersionNumber = existing.VersionNumber + 1,
            ParentDocumentId = existing.Id
        };

        try
        {
            var savedDocument = await _documentService.UpdateDocumentAsync(
                documentId, newDocument, cancellationToken);

            var response = $"Document updated successfully.\n\n";
            response += $"**New Version**: v{savedDocument.VersionNumber}\n";
            response += $"**New ID**: {savedDocument.Id}\n";
            response += $"**File Name**: {savedDocument.FileName}\n";
            response += $"**Previous Version**: v{existing.VersionNumber} (ID: {existing.Id}) — now superseded\n";
            response += $"**Project**: {existing.ProjectId}\n";

            return response;
        }
        catch (InvalidOperationException ex)
        {
            return $"Error: {ex.Message}";
        }
        catch (Exception ex)
        {
            return $"Error updating document: {ex.Message}";
        }
    }

    /// <summary>
    /// Determines the file name for the new version.
    /// If not provided, converts binary formats to .md.
    /// </summary>
    private static string DetermineFileName(Document existing, string? requestedFileName)
    {
        if (!string.IsNullOrWhiteSpace(requestedFileName))
            return requestedFileName;

        // For binary formats, change extension to .md
        var binaryExtensions = new[] { ".pdf", ".docx", ".doc" };
        if (binaryExtensions.Contains(existing.FileExtension.ToLowerInvariant()))
        {
            return Path.ChangeExtension(existing.FileName, ".md");
        }

        // For text formats, keep the same name
        return existing.FileName;
    }

    private static string ComputeHash(byte[] content)
    {
        var hashBytes = SHA256.HashData(content);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
