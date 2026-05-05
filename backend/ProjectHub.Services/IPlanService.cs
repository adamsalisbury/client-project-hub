using ProjectHub.Domain.Models;

namespace ProjectHub.Services;

/// <summary>
/// Project plans: a strictly ordered sequence of <see cref="PlanStep"/> records
/// that can be edited, sent to Claude for verification, and executed step by
/// step. Plans are 1:1 with projects.
/// </summary>
public interface IPlanService
{
    /// <summary>Returns the plan + ordered steps for a project, creating an empty plan on first access.</summary>
    Task<(Plan Plan, IReadOnlyList<PlanStep> Steps)> GetAsync(Guid projectId, CancellationToken cancellationToken);

    /// <summary>Appends a new step to the project's plan.</summary>
    Task<PlanStep> AddStepAsync(Guid projectId, string title, string? description, CancellationToken cancellationToken);

    /// <summary>Updates a step's title and/or description.</summary>
    Task<PlanStep> UpdateStepAsync(Guid projectId, Guid stepId, string title, string? description, CancellationToken cancellationToken);

    /// <summary>Deletes a step and compacts the order of the remaining steps.</summary>
    Task RemoveStepAsync(Guid projectId, Guid stepId, CancellationToken cancellationToken);

    /// <summary>Replaces the order of every step in the plan in one go.</summary>
    Task<IReadOnlyList<PlanStep>> ReorderAsync(Guid projectId, IReadOnlyList<Guid> orderedStepIds, CancellationToken cancellationToken);

    /// <summary>
    /// Queues a chat-mode Claude job that asks Claude to verify the current
    /// plan. The response is stored on the plan and surfaces in the project
    /// chat history as a "Plan Verification Opinion" speech bubble.
    /// </summary>
    Task<ClaudeJob> VerifyAsync(Guid projectId, CancellationToken cancellationToken);

    /// <summary>
    /// Queues an edit-mode Claude job that executes a single step. The worker
    /// captures the changed files into a <see cref="StepReview"/> record once
    /// the job finishes.
    /// </summary>
    Task<ClaudeJob> ExecuteStepAsync(Guid projectId, Guid stepId, CancellationToken cancellationToken);
}
