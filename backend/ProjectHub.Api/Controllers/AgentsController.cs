using ProjectHub.Domain.Models;
using ProjectHub.Services;
using Microsoft.AspNetCore.Mvc;

namespace ProjectHub.Api.Controllers;

[ApiController]
[Route("api/projects/{projectId:guid}/agents")]
public sealed class AgentsController(IAgentService agents) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(Guid projectId, CancellationToken cancellationToken)
    {
        var list = await agents.ListAsync(projectId, cancellationToken);
        return Ok(list.Select(AgentSummary.FromAgent).ToList());
    }

    [HttpGet("{id:guid}", Name = nameof(GetAgent))]
    public async Task<IActionResult> GetAgent(Guid projectId, Guid id, CancellationToken cancellationToken)
    {
        var agent = await agents.GetAsync(projectId, id, cancellationToken);
        return Ok(AgentResponse.FromAgent(agent));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Guid projectId, [FromBody] CreateAgentRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var agent = await agents.CreateAsync(projectId, request.Title, request.Characteristics, cancellationToken);
        return CreatedAtAction(nameof(GetAgent), new { projectId, id = agent.Id }, AgentResponse.FromAgent(agent));
    }

    [HttpPut("{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(Guid projectId, Guid id, [FromBody] CreateAgentRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var agent = await agents.UpdateAsync(projectId, id, request.Title, request.Characteristics, cancellationToken);
        return Ok(AgentResponse.FromAgent(agent));
    }

    [HttpDelete("{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid projectId, Guid id, CancellationToken cancellationToken)
    {
        await agents.DeleteAsync(projectId, id, cancellationToken);
        return NoContent();
    }

    [HttpPost("generate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate(Guid projectId, [FromBody] GenerateAgentRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (title, characteristics) = await agents.GenerateAsync(projectId, request.Title, request.Personality, cancellationToken);
        return Ok(new GeneratedAgentResponse(title, characteristics));
    }
}
