using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mjm.LocalDocs.Core.Models;
using Mjm.LocalDocs.Core.Services;
using Mjm.LocalDocs.Server.Dtos;

namespace Mjm.LocalDocs.Server.Controllers;

/// <summary>
/// API controller for trading system operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class TradingSystemsController : ControllerBase
{
    private readonly TradingSystemService _service;

    public TradingSystemsController(TradingSystemService service)
    {
        _service = service;
    }

    /// <summary>
    /// Gets all trading systems.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TradingSystemListItemResponse>>> GetAll(
        [FromQuery] string? status = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<TradingSystem> systems;

        if (!string.IsNullOrEmpty(search))
        {
            systems = await _service.SearchAsync(search, cancellationToken);
        }
        else if (!string.IsNullOrEmpty(status) && Enum.TryParse<TradingSystemStatus>(status, true, out var statusEnum))
        {
            systems = await _service.GetByStatusAsync(statusEnum, cancellationToken);
        }
        else
        {
            systems = await _service.GetAllAsync(cancellationToken);
        }

        var response = systems.Select(MapToListItem).ToList();
        return Ok(response);
    }

    /// <summary>
    /// Gets a trading system by ID.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<TradingSystemResponse>> GetById(
        string id,
        CancellationToken cancellationToken = default)
    {
        var system = await _service.GetByIdAsync(id, cancellationToken);
        if (system == null)
            return NotFound();

        return Ok(MapToResponse(system));
    }

    /// <summary>
    /// Creates a new trading system.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<TradingSystemResponse>> Create(
        [FromBody] CreateTradingSystemRequest request,
        CancellationToken cancellationToken = default)
    {
        var system = await _service.CreateAsync(
            request.Name,
            request.Description,
            request.SourceUrl,
            request.Tags,
            request.Notes,
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = system.Id }, MapToResponse(system));
    }

    /// <summary>
    /// Updates a trading system's metadata.
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<TradingSystemResponse>> Update(
        string id,
        [FromBody] UpdateTradingSystemRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var system = await _service.UpdateAsync(
                id,
                request.Name,
                request.Description,
                request.SourceUrl,
                request.Tags,
                request.Notes,
                cancellationToken);

            return Ok(MapToResponse(system));
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Updates a trading system's status.
    /// </summary>
    [HttpPatch("{id}/status")]
    public async Task<ActionResult<TradingSystemResponse>> UpdateStatus(
        string id,
        [FromBody] UpdateTradingSystemStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<TradingSystemStatus>(request.Status, true, out var status))
            return BadRequest($"Invalid status: {request.Status}");

        try
        {
            var system = await _service.UpdateStatusAsync(id, status, cancellationToken);
            return Ok(MapToResponse(system));
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Gets the EasyLanguage code for a trading system.
    /// </summary>
    [HttpGet("{id}/code")]
    public async Task<ActionResult<object>> GetCode(
        string id,
        CancellationToken cancellationToken = default)
    {
        var system = await _service.GetByIdAsync(id, cancellationToken);
        if (system == null)
            return NotFound();

        var code = await _service.GetCodeAsync(id, cancellationToken);
        return Ok(new { code = code ?? "" });
    }

    /// <summary>
    /// Saves or updates the EasyLanguage code.
    /// </summary>
    [HttpPut("{id}/code")]
    public async Task<ActionResult<TradingSystemResponse>> SaveCode(
        string id,
        [FromBody] SaveTradingSystemCodeRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var system = await _service.SaveCodeAsync(id, request.Code, cancellationToken);
            return Ok(MapToResponse(system));
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Imports EasyLanguage code from a file.
    /// </summary>
    [HttpPost("{id}/code/import")]
    public async Task<ActionResult<TradingSystemResponse>> ImportCode(
        string id,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
            return BadRequest("File is required.");

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream, cancellationToken);
        var content = stream.ToArray();

        try
        {
            var system = await _service.ImportCodeAsync(id, file.FileName, content, cancellationToken);
            return Ok(MapToResponse(system));
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Exports the EasyLanguage code as a file.
    /// </summary>
    [HttpGet("{id}/code/export")]
    public async Task<IActionResult> ExportCode(
        string id,
        CancellationToken cancellationToken = default)
    {
        var result = await _service.ExportCodeAsync(id, cancellationToken);
        if (result == null)
            return NotFound();

        return File(result.Value.Content, "text/plain", result.Value.FileName);
    }

    /// <summary>
    /// Gets all attachments for a trading system.
    /// </summary>
    [HttpGet("{id}/attachments")]
    public async Task<ActionResult<IReadOnlyList<DocumentResponse>>> GetAttachments(
        string id,
        CancellationToken cancellationToken = default)
    {
        var system = await _service.GetByIdAsync(id, cancellationToken);
        if (system == null)
            return NotFound();

        var attachments = await _service.GetAttachmentsAsync(id, cancellationToken);
        var response = attachments.Select(d => new DocumentResponse(
            d.Id,
            d.ProjectId,
            d.FileName,
            d.FileExtension,
            d.FileSizeBytes,
            d.VersionNumber,
            d.ParentDocumentId,
            d.IsSuperseded,
            d.CreatedAt,
            d.UpdatedAt
        )).ToList();

        return Ok(response);
    }

    /// <summary>
    /// Adds an attachment to a trading system.
    /// </summary>
    [HttpPost("{id}/attachments")]
    public async Task<ActionResult<DocumentResponse>> AddAttachment(
        string id,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
            return BadRequest("File is required.");

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream, cancellationToken);
        var content = stream.ToArray();

        try
        {
            var doc = await _service.AddAttachmentAsync(id, file.FileName, content, cancellationToken);
            return Ok(new DocumentResponse(
                doc.Id,
                doc.ProjectId,
                doc.FileName,
                doc.FileExtension,
                doc.FileSizeBytes,
                doc.VersionNumber,
                doc.ParentDocumentId,
                doc.IsSuperseded,
                doc.CreatedAt,
                doc.UpdatedAt
            ));
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Removes an attachment from a trading system.
    /// </summary>
    [HttpDelete("{id}/attachments/{attachmentId}")]
    public async Task<IActionResult> RemoveAttachment(
        string id,
        string attachmentId,
        CancellationToken cancellationToken = default)
    {
        var success = await _service.RemoveAttachmentAsync(id, attachmentId, cancellationToken);
        if (!success)
            return NotFound();

        return NoContent();
    }

    /// <summary>
    /// Deletes a trading system and all associated data.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(
        string id,
        CancellationToken cancellationToken = default)
    {
        var success = await _service.DeleteAsync(id, cancellationToken);
        if (!success)
            return NotFound();

        return NoContent();
    }

    /// <summary>
    /// Gets available status values.
    /// </summary>
    [HttpGet("statuses")]
    public ActionResult<IReadOnlyList<object>> GetStatuses()
    {
        var statuses = Enum.GetValues<TradingSystemStatus>()
            .Select(s => new { value = s.ToString(), label = FormatStatusLabel(s) })
            .ToList();

        return Ok(statuses);
    }

    private static TradingSystemResponse MapToResponse(TradingSystem system)
    {
        return new TradingSystemResponse(
            system.Id,
            system.Name,
            system.Description,
            system.SourceUrl,
            system.Status.ToString(),
            system.ProjectId,
            system.CodeDocumentId,
            system.AttachmentDocumentIds,
            system.Tags,
            system.Notes,
            system.CreatedAt,
            system.UpdatedAt
        );
    }

    private static TradingSystemListItemResponse MapToListItem(TradingSystem system)
    {
        return new TradingSystemListItemResponse(
            system.Id,
            system.Name,
            system.Description,
            system.Status.ToString(),
            system.Tags,
            system.CodeDocumentId != null,
            system.AttachmentDocumentIds.Count,
            system.CreatedAt,
            system.UpdatedAt
        );
    }

    private static string FormatStatusLabel(TradingSystemStatus status)
    {
        return status switch
        {
            TradingSystemStatus.Draft => "Draft",
            TradingSystemStatus.Backtesting => "Backtesting",
            TradingSystemStatus.Validating => "Validating",
            TradingSystemStatus.Validated => "Validated",
            TradingSystemStatus.Live => "Live",
            TradingSystemStatus.Paused => "Paused",
            TradingSystemStatus.Archived => "Archived",
            _ => status.ToString()
        };
    }
}
