namespace ProjectHub.Domain.Models;

/// <summary>
/// A unit of work persisted by the data provider.
/// Every job belongs to a <see cref="ClaudeProject"/>.
/// </summary>
public sealed class ClaudeJob
{
    public required Guid Id { get; init; }

    public required Guid ProjectId { get; init; }

    public required string Message { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public MessageKind Kind { get; init; } = MessageKind.Chat;

    public JobStatus Status { get; set; } = JobStatus.Queued;

    /// <summary>
    /// The exact prompt string handed to the Claude Code CLI for this job,
    /// including agent personas, project preamble, prior conversation turns,
    /// and the current user message. Captured by the worker just before the
    /// CLI is invoked, so the diagnostics view can show what was actually
    /// sent rather than reconstructing it from possibly-mutated context.
    /// </summary>
    public string? Prompt { get; set; }

    public string? Response { get; set; }

    public string? Error { get; set; }

    public int? ExitCode { get; set; }

    public long? DurationMs { get; set; }

    public DateTimeOffset? StartedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// Files in the project working directory that changed during the run,
    /// as relative paths. Populated by the worker after each invocation.
    /// </summary>
    public IReadOnlyList<string> FilesChanged { get; set; } = Array.Empty<string>();
}
