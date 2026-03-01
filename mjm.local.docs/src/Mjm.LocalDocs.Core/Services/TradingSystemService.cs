using System.Text;
using Mjm.LocalDocs.Core.Abstractions;
using Mjm.LocalDocs.Core.Models;

namespace Mjm.LocalDocs.Core.Services;

/// <summary>
/// Core service for trading system operations.
/// Coordinates between trading system repository, document service, and project repository.
/// </summary>
public sealed class TradingSystemService
{
    /// <summary>
    /// Default project name for trading systems.
    /// </summary>
    public const string DefaultProjectName = "Alma Quant Trading Systems";

    /// <summary>
    /// Default project description.
    /// </summary>
    public const string DefaultProjectDescription = "Collection of EasyLanguage trading systems for TradeStation";

    private readonly ITradingSystemRepository _repository;
    private readonly IDocumentRepository _documentRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly DocumentService _documentService;
    private readonly ITradingSystemAttachmentRepository _attachmentRepository;

    /// <summary>
    /// Creates a new TradingSystemService.
    /// </summary>
    public TradingSystemService(
        ITradingSystemRepository repository,
        IDocumentRepository documentRepository,
        IProjectRepository projectRepository,
        DocumentService documentService,
        ITradingSystemAttachmentRepository attachmentRepository)
    {
        _repository = repository;
        _documentRepository = documentRepository;
        _projectRepository = projectRepository;
        _documentService = documentService;
        _attachmentRepository = attachmentRepository;
    }

    /// <summary>
    /// Gets or creates the default project for trading systems.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The trading systems project.</returns>
    public async Task<Project> GetOrCreateDefaultProjectAsync(CancellationToken cancellationToken = default)
    {
        var project = await _projectRepository.GetByNameAsync(DefaultProjectName, cancellationToken);
        if (project != null)
            return project;

        project = new Project
        {
            Id = Guid.NewGuid().ToString(),
            Name = DefaultProjectName,
            Description = DefaultProjectDescription,
            CreatedAt = DateTimeOffset.UtcNow
        };

        return await _projectRepository.CreateAsync(project, cancellationToken);
    }

    /// <summary>
    /// Creates a new trading system.
    /// </summary>
    /// <param name="name">Name of the trading system.</param>
    /// <param name="description">Optional description.</param>
    /// <param name="sourceUrl">Optional source URL.</param>
    /// <param name="tags">Optional tags.</param>
    /// <param name="notes">Optional notes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created trading system.</returns>
    public async Task<TradingSystem> CreateAsync(
        string name,
        string? description = null,
        string? sourceUrl = null,
        IEnumerable<string>? tags = null,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        var project = await GetOrCreateDefaultProjectAsync(cancellationToken);

        var tradingSystem = new TradingSystem
        {
            Id = Guid.NewGuid().ToString(),
            Name = name.Trim(),
            Description = description?.Trim(),
            SourceUrl = sourceUrl?.Trim(),
            Status = TradingSystemStatus.Draft,
            ProjectId = project.Id,
            Tags = tags?.ToList() ?? [],
            Notes = notes?.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        return await _repository.CreateAsync(tradingSystem, cancellationToken);
    }

    /// <summary>
    /// Gets a trading system by ID.
    /// </summary>
    public Task<TradingSystem?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return _repository.GetByIdAsync(id, cancellationToken);
    }

    /// <summary>
    /// Gets all trading systems.
    /// </summary>
    public Task<IReadOnlyList<TradingSystem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return _repository.GetAllAsync(cancellationToken);
    }

    /// <summary>
    /// Gets trading systems by status.
    /// </summary>
    public Task<IReadOnlyList<TradingSystem>> GetByStatusAsync(
        TradingSystemStatus status,
        CancellationToken cancellationToken = default)
    {
        return _repository.GetByStatusAsync(status, cancellationToken);
    }

    /// <summary>
    /// Searches trading systems.
    /// </summary>
    public Task<IReadOnlyList<TradingSystem>> SearchAsync(
        string searchTerm,
        CancellationToken cancellationToken = default)
    {
        return _repository.SearchAsync(searchTerm, cancellationToken);
    }

    /// <summary>
    /// Updates a trading system's metadata.
    /// </summary>
    public async Task<TradingSystem> UpdateAsync(
        string id,
        string name,
        string? description = null,
        string? sourceUrl = null,
        IEnumerable<string>? tags = null,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        var existing = await _repository.GetByIdAsync(id, cancellationToken);
        if (existing == null)
            throw new InvalidOperationException($"Trading system '{id}' not found.");

        var updated = new TradingSystem
        {
            Id = existing.Id,
            Name = name.Trim(),
            Description = description?.Trim(),
            SourceUrl = sourceUrl?.Trim(),
            Status = existing.Status,
            ProjectId = existing.ProjectId,
            CodeDocumentId = existing.CodeDocumentId,
            AttachmentDocumentIds = existing.AttachmentDocumentIds,
            Tags = tags?.ToList() ?? [],
            Notes = notes?.Trim(),
            CreatedAt = existing.CreatedAt,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        return await _repository.UpdateAsync(updated, cancellationToken);
    }

    /// <summary>
    /// Updates the status of a trading system.
    /// </summary>
    public async Task<TradingSystem> UpdateStatusAsync(
        string id,
        TradingSystemStatus status,
        CancellationToken cancellationToken = default)
    {
        var result = await _repository.UpdateStatusAsync(id, status, cancellationToken);
        if (result == null)
            throw new InvalidOperationException($"Trading system '{id}' not found.");
        return result;
    }

    /// <summary>
    /// Saves or updates the EasyLanguage code for a trading system.
    /// </summary>
    /// <param name="id">Trading system ID.</param>
    /// <param name="code">The EasyLanguage code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated trading system.</returns>
    public async Task<TradingSystem> SaveCodeAsync(
        string id,
        string code,
        CancellationToken cancellationToken = default)
    {
        var tradingSystem = await _repository.GetByIdAsync(id, cancellationToken);
        if (tradingSystem == null)
            throw new InvalidOperationException($"Trading system '{id}' not found.");

        var fileName = $"{SanitizeFileName(tradingSystem.Name)}.el";
        var codeBytes = Encoding.UTF8.GetBytes(code);

        if (tradingSystem.CodeDocumentId != null)
        {
            // Update existing code document
            var newDocument = new Document
            {
                Id = Guid.NewGuid().ToString(),
                ProjectId = tradingSystem.ProjectId,
                FileName = fileName,
                FileExtension = ".el",
                FileContent = codeBytes,
                FileSizeBytes = codeBytes.Length,
                ExtractedText = code,
                VersionNumber = 1,
                ParentDocumentId = tradingSystem.CodeDocumentId,
                CreatedAt = DateTimeOffset.UtcNow
            };

            var savedDoc = await _documentService.UpdateDocumentAsync(
                tradingSystem.CodeDocumentId,
                newDocument,
                cancellationToken);

            await _repository.UpdateCodeDocumentAsync(id, savedDoc.Id, cancellationToken);
        }
        else
        {
            // Create new code document
            var document = new Document
            {
                Id = Guid.NewGuid().ToString(),
                ProjectId = tradingSystem.ProjectId,
                FileName = fileName,
                FileExtension = ".el",
                FileContent = codeBytes,
                FileSizeBytes = codeBytes.Length,
                ExtractedText = code,
                CreatedAt = DateTimeOffset.UtcNow
            };

            var savedDoc = await _documentService.AddDocumentAsync(document, cancellationToken);
            await _repository.UpdateCodeDocumentAsync(id, savedDoc.Id, cancellationToken);
        }

        return (await _repository.GetByIdAsync(id, cancellationToken))!;
    }

    /// <summary>
    /// Gets the EasyLanguage code for a trading system.
    /// </summary>
    /// <param name="id">Trading system ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The code, or null if not set.</returns>
    public async Task<string?> GetCodeAsync(string id, CancellationToken cancellationToken = default)
    {
        var tradingSystem = await _repository.GetByIdAsync(id, cancellationToken);
        if (tradingSystem?.CodeDocumentId == null)
            return null;

        var document = await _documentRepository.GetDocumentAsync(tradingSystem.CodeDocumentId, cancellationToken);
        return document?.ExtractedText;
    }

    /// <summary>
    /// Imports EasyLanguage code from a file.
    /// </summary>
    /// <param name="id">Trading system ID.</param>
    /// <param name="fileName">Original file name.</param>
    /// <param name="fileContent">File content as bytes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated trading system.</returns>
    public async Task<TradingSystem> ImportCodeAsync(
        string id,
        string fileName,
        byte[] fileContent,
        CancellationToken cancellationToken = default)
    {
        var code = Encoding.UTF8.GetString(fileContent);
        return await SaveCodeAsync(id, code, cancellationToken);
    }

    /// <summary>
    /// Exports the EasyLanguage code as bytes.
    /// </summary>
    /// <param name="id">Trading system ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tuple of (fileName, content) or null if no code.</returns>
    public async Task<(string FileName, byte[] Content)?> ExportCodeAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        var tradingSystem = await _repository.GetByIdAsync(id, cancellationToken);
        if (tradingSystem?.CodeDocumentId == null)
            return null;

        var code = await GetCodeAsync(id, cancellationToken);
        if (code == null)
            return null;

        var fileName = $"{SanitizeFileName(tradingSystem.Name)}.el";
        return (fileName, Encoding.UTF8.GetBytes(code));
    }

    /// <summary>
    /// Adds an attachment to a trading system.
    /// </summary>
    /// <param name="id">Trading system ID.</param>
    /// <param name="fileName">Attachment file name.</param>
    /// <param name="fileContent">File content.</param>
    /// <param name="contentType">MIME content type.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created attachment.</returns>
    public async Task<TradingSystemAttachment> AddAttachmentAsync(
        string id,
        string fileName,
        byte[] fileContent,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var tradingSystem = await _repository.GetByIdAsync(id, cancellationToken);
        if (tradingSystem == null)
            throw new InvalidOperationException($"Trading system '{id}' not found.");

        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        var attachment = new TradingSystemAttachment
        {
            Id = Guid.NewGuid().ToString(),
            TradingSystemId = id,
            FileName = fileName,
            FileExtension = extension,
            ContentType = contentType,
            FileSizeBytes = fileContent.Length,
            FileContent = fileContent,
            CreatedAt = DateTimeOffset.UtcNow
        };

        return await _attachmentRepository.CreateAsync(attachment, cancellationToken);
    }

    /// <summary>
    /// Gets all attachments for a trading system.
    /// </summary>
    public Task<IReadOnlyList<TradingSystemAttachment>> GetAttachmentsAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        return _attachmentRepository.GetByTradingSystemIdAsync(id, cancellationToken);
    }

    /// <summary>
    /// Gets a single attachment by ID.
    /// </summary>
    public Task<TradingSystemAttachment?> GetAttachmentByIdAsync(
        string attachmentId,
        CancellationToken cancellationToken = default)
    {
        return _attachmentRepository.GetByIdAsync(attachmentId, cancellationToken);
    }

    /// <summary>
    /// Removes an attachment from a trading system.
    /// </summary>
    public async Task<bool> RemoveAttachmentAsync(
        string id,
        string attachmentId,
        CancellationToken cancellationToken = default)
    {
        var attachment = await _attachmentRepository.GetByIdAsync(attachmentId, cancellationToken);
        if (attachment == null || attachment.TradingSystemId != id)
            return false;

        return await _attachmentRepository.DeleteAsync(attachmentId, cancellationToken);
    }

    /// <summary>
    /// Deletes a trading system and all its associated documents.
    /// </summary>
    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var tradingSystem = await _repository.GetByIdAsync(id, cancellationToken);
        if (tradingSystem == null)
            return false;

        // Delete code document
        if (tradingSystem.CodeDocumentId != null)
        {
            await _documentService.DeleteDocumentAsync(tradingSystem.CodeDocumentId, cancellationToken);
        }

        // Delete all attachments
        await _attachmentRepository.DeleteByTradingSystemIdAsync(id, cancellationToken);

        return await _repository.DeleteAsync(id, cancellationToken);
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new StringBuilder(name.Length);
        foreach (var c in name)
        {
            sanitized.Append(invalid.Contains(c) ? '_' : c);
        }
        return sanitized.ToString();
    }

    private static bool IsTextFile(string extension)
    {
        return extension is ".txt" or ".md" or ".el" or ".csv" or ".json" or ".xml";
    }
}
