namespace ProjectHub.Domain.Models;

/// <summary>
/// A single stage in a project's <see cref="Plan"/>. Steps are linearly
/// dependent: each one assumes every preceding step has been completed.
/// </summary>
public sealed class PlanStep
{
    public required Guid Id { get; init; }

    public required Guid PlanId { get; init; }

    /// <summary>Zero-based position in the plan's step sequence.</summary>
    public required int Order { get; set; }

    public required string Title { get; set; }

    public string Description { get; set; } = string.Empty;

    public PlanStepStatus Status { get; set; } = PlanStepStatus.Pending;

    /// <summary>
    /// The Claude job that most recently executed this step (if any). Lets the
    /// step view link back to the diagnostics for the run.
    /// </summary>
    public Guid? JobId { get; set; }

    public DateTimeOffset? StartedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }
}
