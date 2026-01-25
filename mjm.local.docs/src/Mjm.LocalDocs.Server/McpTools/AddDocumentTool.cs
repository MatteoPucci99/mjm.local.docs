using System.ComponentModel;
using System.Security.Cryptography;
using System.Text;
using ModelContextProtocol.Server;
using Mjm.LocalDocs.Core.Abstractions;
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
    private readonly IProjectRepository _projectRepository;

    public AddDocumentTool(DocumentService documentService, IProjectRepository projectRepository)
    {
        _documentService = documentService;
        _projectRepository = projectRepository;
    }

    [McpServerTool(Name = "add_document")]
    [Description("Add a new text document to a project. The document will be chunked and indexed for semantic search.")]
    public async Task<string> AddDocumentAsync(
        [Description("The project ID to add the document to")] string projectId,
        [Description("File name for the document (e.g., 'FRD_v1.5.txt')")] string fileName,
        [Description("Full text content of the document")] string content,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(projectId))
            return "Error: Project ID is required.";

        if (string.IsNullOrWhiteSpace(fileName))
            return "Error: File name is required.";

        if (string.IsNullOrWhiteSpace(content))
            return "Error: Document content is required.";

        // Verify project exists
        var projectExists = await _projectRepository.ExistsAsync(projectId, cancellationToken);
        if (!projectExists)
        {
            return $"Error: Project with ID '{projectId}' not found. Create it first using create_project.";
        }

        // Get file extension
        var fileExtension = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(fileExtension))
        {
            fileExtension = ".txt";
            fileName += fileExtension;
        }

        // For now only support .txt files
        if (!fileExtension.Equals(".txt", StringComparison.OrdinalIgnoreCase))
        {
            return $"Error: Currently only .txt files are supported. Got '{fileExtension}'.";
        }

        // Convert content to bytes
        var fileContent = Encoding.UTF8.GetBytes(content);

        // Calculate content hash for deduplication
        var contentHash = ComputeHash(fileContent);

        var document = new Document
        {
            Id = Guid.NewGuid().ToString(),
            ProjectId = projectId,
            FileName = fileName,
            FileExtension = fileExtension,
            FileContent = fileContent,
            FileSizeBytes = fileContent.Length,
            ExtractedText = content,
            ContentHash = contentHash
        };

        try
        {
            var savedDocument = await _documentService.AddDocumentAsync(document, cancellationToken);
            return $"Document '{fileName}' (ID: {savedDocument.Id}) successfully added to project '{projectId}'.";
        }
        catch (Exception ex)
        {
            return $"Error adding document: {ex.Message}";
        }
    }

    private static string ComputeHash(byte[] content)
    {
        var hashBytes = SHA256.HashData(content);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
