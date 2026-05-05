using ProjectHub.Domain.Models;
using ProjectHub.Services;
using Microsoft.AspNetCore.Mvc;

namespace ProjectHub.Api.Controllers;

/// <summary>
/// CRUD + verification + execution endpoints for a project's plan. The plan
/// itself is created lazily on first <c>GET</c>.
/// </summary>
[ApiController]
[Route("api/projects/{projectId:guid}/plan")]
public sealed class ProjectPlanController(IPlanService plans) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(Guid projectId, CancellationToken cancellationToken)
    {
        var (plan, steps) = await plans.GetAsync(projectId, cancellationToken);
        return Ok(PlanResponse.FromPlan(plan, steps));
    }

    [HttpPost("steps")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddStep(Guid projectId, [FromBody] CreatePlanStepRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var step = await plans.AddStepAsync(projectId, request.Title, request.Description, cancellationToken);
        return Ok(PlanStepResponse.FromStep(step));
    }

    [HttpPut("steps/{stepId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStep(Guid projectId, Guid stepId, [FromBody] UpdatePlanStepRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var step = await plans.UpdateStepAsync(projectId, stepId, request.Title, request.Description, cancellationToken);
        return Ok(PlanStepResponse.FromStep(step));
    }

    [HttpDelete("steps/{stepId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteStep(Guid projectId, Guid stepId, CancellationToken cancellationToken)
    {
        await plans.RemoveStepAsync(projectId, stepId, cancellationToken);
        return NoContent();
    }

    [HttpPut("order")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reorder(Guid projectId, [FromBody] ReorderPlanStepsRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var steps = await plans.ReorderAsync(projectId, request.StepIds, cancellationToken);
        return Ok(steps.Select(PlanStepResponse.FromStep).ToList());
    }

    [HttpPost("verify")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Verify(Guid projectId, CancellationToken cancellationToken)
    {
        var job = await plans.VerifyAsync(projectId, cancellationToken);
        return Accepted(new ClaudeJobSubmittedResponse(job.Id, job.ProjectId, job.Kind, job.Status));
    }

    [HttpPost("steps/{stepId:guid}/execute")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExecuteStep(Guid projectId, Guid stepId, CancellationToken cancellationToken)
    {
        var job = await plans.ExecuteStepAsync(projectId, stepId, cancellationToken);
        return Accepted(new ClaudeJobSubmittedResponse(job.Id, job.ProjectId, job.Kind, job.Status));
    }
}
