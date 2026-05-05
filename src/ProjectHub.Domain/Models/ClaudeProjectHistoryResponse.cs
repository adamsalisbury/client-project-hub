namespace ProjectHub.Domain.Models;

/// <summary>
/// Wire format for the full message history of a project.
/// </summary>
public sealed record ClaudeProjectHistoryResponse(
    ClaudeProjectResponse Project,
    IReadOnlyList<ClaudeProjectMessage> Messages);

/// <summary>
/// A single user-message-and-assistant-response pair within a project, with
/// the timestamps of the message and the response.
/// </summary>
public sealed record ClaudeProjectMessage(
    Guid Id,
    MessageKind Kind,
    JobStatus Status,
    string Message,
    DateTimeOffset MessageAt,
    string? Response,
    DateTimeOffset? ResponseAt,
    int? ExitCode,
    long? DurationMs,
    string? Error,
    string? Prompt,
    IReadOnlyList<string> FilesChanged)
{
    public static ClaudeProjectMessage FromJob(ClaudeJob job) => new(
        job.Id,
        job.Kind,
        job.Status,
        job.Message,
        job.CreatedAt,
        job.Response,
        job.CompletedAt,
        job.ExitCode,
        job.DurationMs,
        job.Error,
        job.Prompt,
        job.FilesChanged);
}
