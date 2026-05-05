using ProjectHub.Domain.Models;

namespace ProjectHub.Services;

/// <summary>
/// Read + reconcile the file-by-file outcome of a plan step's execution.
/// Each <see cref="StepReview"/> snapshots the files Claude changed; the user
/// then commits or rolls back individual files using git operations on the
/// project's working directory.
/// </summary>
public interface IStepReviewService
{
    /// <summary>Lists every step review owned by a project, newest first.</summary>
    Task<IReadOnlyList<StepReview>> ListByProjectAsync(Guid projectId, CancellationToken cancellationToken);

    /// <summary>Retrieves a single review by id.</summary>
    Task<StepReview> GetAsync(Guid reviewId, CancellationToken cancellationToken);

    /// <summary>
    /// Stages and commits a single file's pending changes against HEAD with
    /// the supplied message (defaulted from the step title when omitted).
    /// </summary>
    Task<StepReview> CommitFileAsync(Guid reviewId, string path, string? message, CancellationToken cancellationToken);

    /// <summary>
    /// Reverts a single file's pending changes to its last-committed state
    /// (or removes it when it was untracked) via git.
    /// </summary>
    Task<StepReview> RollbackFileAsync(Guid reviewId, string path, CancellationToken cancellationToken);
}
