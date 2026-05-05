using ProjectHub.Domain.Models;
using ProjectHub.Services;
using Microsoft.AspNetCore.Mvc;

namespace ProjectHub.Api.Controllers;

/// <summary>
/// Per-file commit / rollback for the changes Claude made during a plan
/// step's execution.
/// </summary>
[ApiController]
[Route("api/projects/{projectId:guid}/step-reviews")]
public sealed class StepReviewController(IStepReviewService reviews) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(Guid projectId, CancellationToken cancellationToken)
    {
        var list = await reviews.ListByProjectAsync(projectId, cancellationToken);
        return Ok(list.Select(StepReviewResponse.FromReview).ToList());
    }

    [HttpGet("{reviewId:guid}")]
    public async Task<IActionResult> Get(Guid projectId, Guid reviewId, CancellationToken cancellationToken)
    {
        var review = await reviews.GetAsync(reviewId, cancellationToken);
        return Ok(StepReviewResponse.FromReview(review));
    }

    [HttpPost("{reviewId:guid}/commit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Commit(Guid projectId, Guid reviewId, [FromBody] CommitFileRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var review = await reviews.CommitFileAsync(reviewId, request.Path, request.Message, cancellationToken);
        return Ok(StepReviewResponse.FromReview(review));
    }

    [HttpPost("{reviewId:guid}/rollback")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rollback(Guid projectId, Guid reviewId, [FromBody] RollbackFileRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var review = await reviews.RollbackFileAsync(reviewId, request.Path, cancellationToken);
        return Ok(StepReviewResponse.FromReview(review));
    }
}
