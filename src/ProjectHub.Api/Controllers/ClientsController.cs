using ProjectHub.Domain.Models;
using ProjectHub.Services;
using Microsoft.AspNetCore.Mvc;

namespace ProjectHub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ClientsController(
    IClientService clients,
    IKnowledgeService knowledge) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var list = await clients.ListAsync(cancellationToken);
        return Ok(list.Select(ClientResponse.FromClient).ToList());
    }

    [HttpGet("{id:guid}", Name = nameof(GetClient))]
    public async Task<IActionResult> GetClient(Guid id, CancellationToken cancellationToken)
        => Ok(ClientResponse.FromClient(await clients.GetAsync(id, cancellationToken)));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromBody] CreateClientRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var client = await clients.CreateAsync(request.Name, cancellationToken);
        return CreatedAtAction(nameof(GetClient), new { id = client.Id }, ClientResponse.FromClient(client));
    }

    [HttpGet("{id:guid}/projects")]
    public async Task<IActionResult> ListProjects(Guid id, CancellationToken cancellationToken)
    {
        var list = await clients.ListProjectsAsync(id, cancellationToken);
        return Ok(list.Select(ClaudeProjectResponse.FromProject).ToList());
    }

    [HttpGet("{clientId:guid}/knowledge")]
    public async Task<IActionResult> ListKnowledge(Guid clientId, CancellationToken cancellationToken)
    {
        var list = await knowledge.ListClientKnowledgeAsync(clientId, cancellationToken);
        return Ok(list.Select(KnowledgeSummary.FromEntry).ToList());
    }

    [HttpGet("{clientId:guid}/knowledge/{id:guid}", Name = nameof(GetKnowledge))]
    public async Task<IActionResult> GetKnowledge(Guid clientId, Guid id, CancellationToken cancellationToken)
    {
        var entry = await knowledge.GetClientKnowledgeAsync(clientId, id, cancellationToken);
        return Ok(KnowledgeResponse.FromEntry(entry));
    }

    [HttpPost("{clientId:guid}/knowledge")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateKnowledge(Guid clientId, [FromBody] CreateKnowledgeRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entry = await knowledge.CreateClientKnowledgeAsync(clientId, request.Title, request.Body, cancellationToken);
        return CreatedAtAction(nameof(GetKnowledge), new { clientId, id = entry.Id }, KnowledgeResponse.FromEntry(entry));
    }

    [HttpDelete("{clientId:guid}/knowledge/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteKnowledge(Guid clientId, Guid id, CancellationToken cancellationToken)
    {
        await knowledge.DeleteClientKnowledgeAsync(clientId, id, cancellationToken);
        return NoContent();
    }
}
