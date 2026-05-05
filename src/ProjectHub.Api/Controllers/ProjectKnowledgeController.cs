using ProjectHub.Domain.Models;
using ProjectHub.Services;
using Microsoft.AspNetCore.Mvc;

namespace ProjectHub.Api.Controllers;

[ApiController]
[Route("api/projects/{projectId:guid}/knowledge")]
public sealed class ProjectKnowledgeController(IKnowledgeService knowledge) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(Guid projectId, CancellationToken cancellationToken)
    {
        var list = await knowledge.ListProjectKnowledgeAsync(projectId, cancellationToken);
        return Ok(list.Select(KnowledgeSummary.FromEntry).ToList());
    }

    [HttpGet("{id:guid}", Name = nameof(GetEntry))]
    public async Task<IActionResult> GetEntry(Guid projectId, Guid id, CancellationToken cancellationToken)
    {
        var entry = await knowledge.GetProjectKnowledgeAsync(projectId, id, cancellationToken);
        return Ok(KnowledgeResponse.FromEntry(entry));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Guid projectId, [FromBody] CreateKnowledgeRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entry = await knowledge.CreateProjectKnowledgeAsync(projectId, request.Title, request.Body, cancellationToken);
        return CreatedAtAction(nameof(GetEntry), new { projectId, id = entry.Id }, KnowledgeResponse.FromEntry(entry));
    }

    [HttpDelete("{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid projectId, Guid id, CancellationToken cancellationToken)
    {
        await knowledge.DeleteProjectKnowledgeAsync(projectId, id, cancellationToken);
        return NoContent();
    }
}
