using ProjectHub.Domain.Models;
using ProjectHub.Services;
using Microsoft.AspNetCore.Mvc;

namespace ProjectHub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ClaudeController(IClaudeJobService jobs) : ControllerBase
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit([FromBody] ClaudeRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var job = await jobs.SubmitAsync(request.ProjectId, request.Message ?? string.Empty, request.Kind, cancellationToken);
        var response = new ClaudeJobSubmittedResponse(job.Id, job.ProjectId, job.Kind, job.Status);
        return AcceptedAtAction(nameof(GetJob), new { id = job.Id }, response);
    }

    [HttpGet("{id:guid}", Name = nameof(GetJob))]
    public async Task<IActionResult> GetJob(Guid id, CancellationToken cancellationToken)
    {
        var job = await jobs.GetAsync(id, cancellationToken);
        return Ok(ClaudeJobStatusResponse.FromJob(job));
    }
}
