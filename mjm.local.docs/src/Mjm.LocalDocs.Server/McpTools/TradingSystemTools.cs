using System.ComponentModel;
using System.Text;
using System.Text.Json;
using ModelContextProtocol.Server;
using Mjm.LocalDocs.Core.Models;
using Mjm.LocalDocs.Core.Services;

namespace Mjm.LocalDocs.Server.McpTools;

/// <summary>
/// MCP tools for trading system operations.
/// </summary>
[McpServerToolType]
public sealed class TradingSystemTools
{
    private readonly TradingSystemService _service;

    public TradingSystemTools(TradingSystemService service)
    {
        _service = service;
    }

    [McpServerTool(Name = "list_trading_systems")]
    [Description("List all trading systems with optional status filter. Returns name, status, description, and tags for each system.")]
    public async Task<string> ListTradingSystemsAsync(
        [Description("Filter by status: draft, backtesting, validating, validated, live, paused, archived. Leave empty for all.")]
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            IReadOnlyList<TradingSystem> systems;

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<TradingSystemStatus>(status, true, out var statusEnum))
            {
                systems = await _service.GetByStatusAsync(statusEnum, cancellationToken);
            }
            else
            {
                systems = await _service.GetAllAsync(cancellationToken);
            }

            if (systems.Count == 0)
                return "No trading systems found.";

            var sb = new StringBuilder();
            sb.AppendLine($"Found {systems.Count} trading system(s):\n");

            foreach (var ts in systems)
            {
                sb.AppendLine($"- **{ts.Name}** (ID: {ts.Id})");
                sb.AppendLine($"  Status: {ts.Status}");
                if (!string.IsNullOrEmpty(ts.Description))
                    sb.AppendLine($"  Description: {ts.Description}");
                if (ts.Tags.Count > 0)
                    sb.AppendLine($"  Tags: {string.Join(", ", ts.Tags)}");
                sb.AppendLine($"  Has Code: {(ts.CodeDocumentId != null ? "Yes" : "No")}");
                sb.AppendLine();
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            return $"Error listing trading systems: {ex.Message}";
        }
    }

    [McpServerTool(Name = "get_trading_system")]
    [Description("Get detailed information about a trading system including its EasyLanguage code.")]
    public async Task<string> GetTradingSystemAsync(
        [Description("The trading system ID")] string id,
        [Description("Whether to include the full code in the response. Default: true")] bool includeCode = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var system = await _service.GetByIdAsync(id, cancellationToken);
            if (system == null)
                return $"Trading system '{id}' not found.";

            var sb = new StringBuilder();
            sb.AppendLine($"# {system.Name}");
            sb.AppendLine();
            sb.AppendLine($"**ID:** {system.Id}");
            sb.AppendLine($"**Status:** {system.Status}");

            if (!string.IsNullOrEmpty(system.Description))
                sb.AppendLine($"**Description:** {system.Description}");

            if (!string.IsNullOrEmpty(system.SourceUrl))
                sb.AppendLine($"**Source URL:** {system.SourceUrl}");

            if (system.Tags.Count > 0)
                sb.AppendLine($"**Tags:** {string.Join(", ", system.Tags)}");

            sb.AppendLine($"**Created:** {system.CreatedAt:yyyy-MM-dd HH:mm}");
            if (system.UpdatedAt != null)
                sb.AppendLine($"**Updated:** {system.UpdatedAt:yyyy-MM-dd HH:mm}");

            sb.AppendLine($"**Attachments:** {system.AttachmentDocumentIds.Count}");

            if (!string.IsNullOrEmpty(system.Notes))
            {
                sb.AppendLine();
                sb.AppendLine("## Notes");
                sb.AppendLine(system.Notes);
            }

            if (includeCode && system.CodeDocumentId != null)
            {
                var code = await _service.GetCodeAsync(id, cancellationToken);
                if (!string.IsNullOrEmpty(code))
                {
                    sb.AppendLine();
                    sb.AppendLine("## EasyLanguage Code");
                    sb.AppendLine("```easylanguage");
                    sb.AppendLine(code);
                    sb.AppendLine("```");
                }
            }
            else if (system.CodeDocumentId == null)
            {
                sb.AppendLine();
                sb.AppendLine("*No code has been added yet.*");
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            return $"Error getting trading system: {ex.Message}";
        }
    }

    [McpServerTool(Name = "search_trading_systems")]
    [Description("Search trading systems by name, description, or notes.")]
    public async Task<string> SearchTradingSystemsAsync(
        [Description("The search query")] string query,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var systems = await _service.SearchAsync(query, cancellationToken);

            if (systems.Count == 0)
                return $"No trading systems found matching '{query}'.";

            var sb = new StringBuilder();
            sb.AppendLine($"Found {systems.Count} trading system(s) matching '{query}':\n");

            foreach (var ts in systems)
            {
                sb.AppendLine($"- **{ts.Name}** (ID: {ts.Id}) - Status: {ts.Status}");
                if (!string.IsNullOrEmpty(ts.Description))
                    sb.AppendLine($"  {ts.Description}");
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            return $"Error searching trading systems: {ex.Message}";
        }
    }

    [McpServerTool(Name = "create_trading_system")]
    [Description("Create a new trading system.")]
    public async Task<string> CreateTradingSystemAsync(
        [Description("Name of the trading system")] string name,
        [Description("Description of the strategy")] string? description = null,
        [Description("URL to the source/reference material")] string? sourceUrl = null,
        [Description("Comma-separated tags for categorization")] string? tags = null,
        [Description("Additional notes")] string? notes = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var tagList = string.IsNullOrEmpty(tags)
                ? null
                : tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

            var system = await _service.CreateAsync(name, description, sourceUrl, tagList, notes, cancellationToken);

            return $"Trading system '{system.Name}' created successfully.\n\n" +
                   $"**ID:** {system.Id}\n" +
                   $"**Status:** {system.Status}\n" +
                   $"**Project ID:** {system.ProjectId}";
        }
        catch (Exception ex)
        {
            return $"Error creating trading system: {ex.Message}";
        }
    }

    [McpServerTool(Name = "update_trading_system_status")]
    [Description("Update the status of a trading system (draft, backtesting, validating, validated, live, paused, archived).")]
    public async Task<string> UpdateTradingSystemStatusAsync(
        [Description("The trading system ID")] string id,
        [Description("New status: draft, backtesting, validating, validated, live, paused, archived")] string status,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Enum.TryParse<TradingSystemStatus>(status, true, out var statusEnum))
                return $"Invalid status '{status}'. Valid values: draft, backtesting, validating, validated, live, paused, archived";

            var system = await _service.UpdateStatusAsync(id, statusEnum, cancellationToken);
            return $"Trading system '{system.Name}' status updated to **{system.Status}**.";
        }
        catch (InvalidOperationException ex)
        {
            return ex.Message;
        }
        catch (Exception ex)
        {
            return $"Error updating status: {ex.Message}";
        }
    }

    [McpServerTool(Name = "save_trading_system_code")]
    [Description("Save or update the EasyLanguage code for a trading system.")]
    public async Task<string> SaveTradingSystemCodeAsync(
        [Description("The trading system ID")] string id,
        [Description("The EasyLanguage code")] string code,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var system = await _service.SaveCodeAsync(id, code, cancellationToken);
            return $"Code saved successfully for trading system '{system.Name}'.\n" +
                   $"Code document ID: {system.CodeDocumentId}";
        }
        catch (InvalidOperationException ex)
        {
            return ex.Message;
        }
        catch (Exception ex)
        {
            return $"Error saving code: {ex.Message}";
        }
    }

    [McpServerTool(Name = "get_trading_system_code")]
    [Description("Get the EasyLanguage code for a trading system.")]
    public async Task<string> GetTradingSystemCodeAsync(
        [Description("The trading system ID")] string id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var system = await _service.GetByIdAsync(id, cancellationToken);
            if (system == null)
                return $"Trading system '{id}' not found.";

            var code = await _service.GetCodeAsync(id, cancellationToken);
            if (string.IsNullOrEmpty(code))
                return $"No code has been added to trading system '{system.Name}' yet.";

            return $"# {system.Name} - EasyLanguage Code\n\n```easylanguage\n{code}\n```";
        }
        catch (Exception ex)
        {
            return $"Error getting code: {ex.Message}";
        }
    }

    [McpServerTool(Name = "delete_trading_system")]
    [Description("Delete a trading system and all its associated data (code, attachments).")]
    public async Task<string> DeleteTradingSystemAsync(
        [Description("The trading system ID")] string id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var system = await _service.GetByIdAsync(id, cancellationToken);
            if (system == null)
                return $"Trading system '{id}' not found.";

            var name = system.Name;
            var success = await _service.DeleteAsync(id, cancellationToken);

            if (success)
                return $"Trading system '{name}' deleted successfully.";
            else
                return $"Failed to delete trading system '{id}'.";
        }
        catch (Exception ex)
        {
            return $"Error deleting trading system: {ex.Message}";
        }
    }
}
