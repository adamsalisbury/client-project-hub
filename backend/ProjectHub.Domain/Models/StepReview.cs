namespace ProjectHub.Domain.Models;

/// <summary>
/// A snapshot of files touched by a plan step's execution, with per-file
/// commit / rollback state tracked by the user. One review row per step
/// invocation.
/// </summary>
public sealed class StepReview
{
    public required Guid Id { get; init; }

    public required Guid ProjectId { get; init; }

    public required Guid StepId { get; init; }

    /// <summary>The job whose run produced these changed files.</summary>
    public required Guid JobId { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public List<StepReviewFile> Files { get; set; } = new();
}

/// <summary>One file in a <see cref="StepReview"/>.</summary>
public sealed class StepReviewFile
{
    /// <summary>Path relative to the project working directory.</summary>
    public required string Path { get; init; }

    public StepReviewFileState State { get; set; } = StepReviewFileState.Pending;

    public DateTimeOffset? ResolvedAt { get; set; }
}

/// <summary>Per-file state in a step review.</summary>
public enum StepReviewFileState
{
    /// <summary>Awaiting user decision.</summary>
    Pending,

    /// <summary>Staged + committed to git.</summary>
    Committed,

    /// <summary>Reverted to the last committed state via <c>git checkout</c>.</summary>
    RolledBack
}
