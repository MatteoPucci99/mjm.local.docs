using System.ComponentModel;
using ModelContextProtocol.Server;
using Mjm.LocalDocs.Core.Abstractions;

namespace Mjm.LocalDocs.Server.McpTools;

/// <summary>
/// MCP Tool for listing available projects.
/// </summary>
[McpServerToolType]
public sealed class ListProjectsTool
{
    private readonly IProjectRepository _projectRepository;

    public ListProjectsTool(IProjectRepository projectRepository)
    {
        _projectRepository = projectRepository;
    }

    [McpServerTool(Name = "list_projects")]
    [Description("List all available projects in the documentation store.")]
    public async Task<string> ListProjectsAsync(CancellationToken cancellationToken = default)
    {
        var projects = await _projectRepository.GetAllAsync(cancellationToken);

        if (projects.Count == 0)
        {
            return "No projects found. Create a project using the create_project tool first.";
        }

        var response = $"Available projects ({projects.Count}):\n\n";

        foreach (var project in projects)
        {
            response += $"- **{project.Name}** (ID: {project.Id})\n";
            if (!string.IsNullOrEmpty(project.Description))
            {
                response += $"  {project.Description}\n";
            }
        }

        return response;
    }
}
