namespace ProjectHub.Domain.Models;

/// <summary>
/// The development plan associated with a project. Each project has at most
/// one plan; the plan is a strictly ordered sequence of <see cref="PlanStep"/>
/// records where step <c>n + 1</c> depends on step <c>n</c>. Plans are
/// editable manually and can be sent to Claude for verification before
/// execution.
/// </summary>
public sealed class Plan
{
    public required Guid Id { get; init; }

    public required Guid ProjectId { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>
    /// The most recent verification opinion returned by Claude, displayed in
    /// the chat as a "Plan Verification Opinion" speech bubble. Null until
    /// the plan has been verified at least once.
    /// </summary>
    public string? LastVerificationOpinion { get; set; }

    public DateTimeOffset? LastVerifiedAt { get; set; }
}
