namespace ProjectHub.Domain.Models;

/// <summary>Wire format for a single plan step.</summary>
public sealed record PlanStepResponse(
    Guid Id,
    Guid PlanId,
    int Order,
    string Title,
    string Description,
    PlanStepStatus Status,
    Guid? JobId,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt)
{
    public static PlanStepResponse FromStep(PlanStep step)
        => new(step.Id, step.PlanId, step.Order, step.Title, step.Description,
               step.Status, step.JobId, step.StartedAt, step.CompletedAt);
}

/// <summary>Wire format for the plan attached to a project.</summary>
public sealed record PlanResponse(
    Guid Id,
    Guid ProjectId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    string? LastVerificationOpinion,
    DateTimeOffset? LastVerifiedAt,
    IReadOnlyList<PlanStepResponse> Steps)
{
    public static PlanResponse FromPlan(Plan plan, IEnumerable<PlanStep> steps)
    {
        var ordered = steps
            .OrderBy(s => s.Order)
            .Select(PlanStepResponse.FromStep)
            .ToList();

        return new PlanResponse(plan.Id, plan.ProjectId, plan.CreatedAt, plan.UpdatedAt,
                                plan.LastVerificationOpinion, plan.LastVerifiedAt, ordered);
    }
}

/// <summary>Request body for <c>POST /api/projects/{id}/plan/steps</c>.</summary>
public sealed record CreatePlanStepRequest(string Title, string? Description);

/// <summary>Request body for <c>PUT /api/projects/{id}/plan/steps/{stepId}</c>.</summary>
public sealed record UpdatePlanStepRequest(string Title, string? Description);

/// <summary>Request body for <c>PUT /api/projects/{id}/plan/order</c>.</summary>
public sealed record ReorderPlanStepsRequest(IReadOnlyList<Guid> StepIds);

/// <summary>Wire format for a step review.</summary>
public sealed record StepReviewResponse(
    Guid Id,
    Guid ProjectId,
    Guid StepId,
    Guid JobId,
    DateTimeOffset CreatedAt,
    IReadOnlyList<StepReviewFileResponse> Files)
{
    public static StepReviewResponse FromReview(StepReview review)
    {
        var files = review.Files.Select(f => new StepReviewFileResponse(f.Path, f.State, f.ResolvedAt)).ToList();
        return new StepReviewResponse(review.Id, review.ProjectId, review.StepId, review.JobId, review.CreatedAt, files);
    }
}

public sealed record StepReviewFileResponse(string Path, StepReviewFileState State, DateTimeOffset? ResolvedAt);

/// <summary>Request body for <c>POST /api/projects/{id}/step-reviews/{reviewId}/files/{path}/commit</c>.</summary>
public sealed record CommitFileRequest(string Path, string? Message);

/// <summary>Request body for the rollback endpoint.</summary>
public sealed record RollbackFileRequest(string Path);
