using ProjectHub.Domain.Models;
using ProjectHub.Services;
using Microsoft.AspNetCore.Mvc;

namespace ProjectHub.Api.Controllers;

/// <summary>
/// Web facade for project lifecycle, history, client attachment, memory
/// usage, and memory selection. All work is delegated to <see cref="IProjectService"/>.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class ProjectsController(IProjectService projects) : ControllerBase
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromBody] CreateProjectRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var project = await projects.CreateAsync(request.Name ?? string.Empty, request.WorkingDirectory ?? string.Empty, request.ClientId, cancellationToken);
        var response = ClaudeProjectResponse.FromProject(project);
        return CreatedAtAction(nameof(GetHistory), new { id = project.Id }, response);
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var list = await projects.ListAsync(cancellationToken);
        return Ok(list.Select(ClaudeProjectResponse.FromProject).ToList());
    }

    [HttpGet("{id:guid}/history", Name = nameof(GetHistory))]
    public async Task<IActionResult> GetHistory(Guid id, CancellationToken cancellationToken)
    {
        var result = await projects.GetHistoryAsync(id, cancellationToken);
        if (result is null) return NotFound(new { error = $"No project found with id {id}." });
        var (project, jobs) = result.Value;
        var messages = jobs.Select(ClaudeProjectMessage.FromJob).ToList();
        return Ok(new ClaudeProjectHistoryResponse(ClaudeProjectResponse.FromProject(project), messages));
    }

    [HttpPut("{id:guid}/client")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignClient(Guid id, [FromBody] AssignClientRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var project = await projects.AssignClientAsync(id, request.ClientId, cancellationToken);
        return Ok(ClaudeProjectResponse.FromProject(project));
    }

    [HttpGet("{id:guid}/memory-usage")]
    public async Task<IActionResult> GetMemoryUsage(Guid id, CancellationToken cancellationToken)
        => Ok(await projects.GetMemoryUsageAsync(id, cancellationToken));

    [HttpGet("{id:guid}/memory-selection")]
    public async Task<IActionResult> GetMemorySelection(Guid id, CancellationToken cancellationToken)
        => Ok(MemorySelectionResponse.FromSelection(await projects.GetMemorySelectionAsync(id, cancellationToken)));

    [HttpPut("{id:guid}/memory-selection")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateMemorySelection(Guid id, [FromBody] UpdateMemorySelectionRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var selection = new MemorySelection
        {
            IncludeProjectInfo = request.IncludeProjectInfo,
            IncludeClientInfo = request.IncludeClientInfo,
            ExcludedAgentIds = request.ExcludedAgentIds?.ToList() ?? new(),
            ExcludedTicketIds = request.ExcludedTicketIds?.ToList() ?? new(),
            ExcludedProjectKnowledgeIds = request.ExcludedProjectKnowledgeIds?.ToList() ?? new(),
            ExcludedClientKnowledgeIds = request.ExcludedClientKnowledgeIds?.ToList() ?? new(),
            ExcludedConversationJobIds = request.ExcludedConversationJobIds?.ToList() ?? new()
        };
        var saved = await projects.UpdateMemorySelectionAsync(id, selection, cancellationToken);
        return Ok(MemorySelectionResponse.FromSelection(saved));
    }
}
