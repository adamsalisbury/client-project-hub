namespace ProjectHub.Domain.Models;

/// <summary>
/// Returned from <c>GET /api/claude/{id}</c>. Carries the current status and,
/// once <see cref="JobStatus.Completed"/> or <see cref="JobStatus.Failed"/>,
/// the result of the run.
/// </summary>
public sealed record ClaudeJobStatusResponse(
    Guid Id,
    Guid ProjectId,
    MessageKind Kind,
    JobStatus Status,
    string Message,
    DateTimeOffset MessageAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? ResponseAt,
    string? Response,
    int? ExitCode,
    long? DurationMs,
    string? Error,
    IReadOnlyList<string> FilesChanged)
{
    public static ClaudeJobStatusResponse FromJob(ClaudeJob job) => new(
        job.Id,
        job.ProjectId,
        job.Kind,
        job.Status,
        job.Message,
        job.CreatedAt,
        job.StartedAt,
        job.CompletedAt,
        job.Response,
        job.ExitCode,
        job.DurationMs,
        job.Error,
        job.FilesChanged);
}
