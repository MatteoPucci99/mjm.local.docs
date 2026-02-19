using System.ComponentModel;
using System.Security.Cryptography;
using System.Text;
using ModelContextProtocol.Server;
using Mjm.LocalDocs.Core.Abstractions;
using Mjm.LocalDocs.Core.Models;
using Mjm.LocalDocs.Core.Services;
using Mjm.LocalDocs.Infrastructure.Documents;

namespace Mjm.LocalDocs.Server.McpTools;

/// <summary>
/// MCP Tool for adding documents to the store.
/// </summary>
[McpServerToolType]
public sealed class AddDocumentTool
{
    private readonly DocumentService _documentService;
    private readonly IProjectRepository _projectRepository;
    private readonly CompositeDocumentReader _documentReader;

    public AddDocumentTool(
        DocumentService documentService,
        IProjectRepository projectRepository,
        CompositeDocumentReader documentReader)
    {
        _documentService = documentService;
        _projectRepository = projectRepository;
        _documentReader = documentReader;
    }

    [McpServerTool(Name = "add_document")]
    [Description("Add a document to a project. Supports .txt, .md, .pdf, and .docx files. The document will be chunked and indexed for semantic search.")]
    public async Task<string> AddDocumentAsync(
        [Description("The project ID to add the document to")] string projectId,
        [Description("File name for the document (e.g., 'FRD_v1.5.txt', 'notes.md', 'spec.pdf', 'requirements.docx')")] string fileName,
        [Description("Full text content of the document (for .txt and .md files) or base64-encoded content (for binary files like .pdf, .docx)")] string content,
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

        // Check if the format is supported
        if (!_documentReader.CanRead(fileExtension))
        {
            var supportedFormats = string.Join(", ", _documentReader.SupportedExtensions);
            return $"Error: Unsupported file format '{fileExtension}'. Supported formats: {supportedFormats}";
        }

        // Convert content to bytes based on file type
        byte[] fileContent;
        try
        {
            fileContent = GetFileContent(content, fileExtension);
        }
        catch (FormatException)
        {
            return $"Error: Invalid base64-encoded content for '{fileName}'. Binary files (like PDF) must be base64-encoded.";
        }

        // Extract text from document
        var extractionResult = await _documentReader.ExtractTextAsync(fileContent, fileExtension, cancellationToken);
        if (!extractionResult.Success)
        {
            return $"Error: Unable to extract text from '{fileName}'. {extractionResult.ErrorMessage}";
        }

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
            ExtractedText = extractionResult.Text,
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

    private static byte[] GetFileContent(string content, string fileExtension)
    {
        // For text-based files, content is plain text - convert to UTF-8 bytes
        if (fileExtension.Equals(".txt", StringComparison.OrdinalIgnoreCase) ||
            fileExtension.Equals(".md", StringComparison.OrdinalIgnoreCase))
        {
            return Encoding.UTF8.GetBytes(content);
        }

        // For binary files (PDF, etc.), content should be base64-encoded
        return Convert.FromBase64String(content);
    }

    private static string ComputeHash(byte[] content)
    {
        var hashBytes = SHA256.HashData(content);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
