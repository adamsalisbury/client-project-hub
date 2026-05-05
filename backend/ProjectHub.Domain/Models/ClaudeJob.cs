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

    /// <summary>
    /// Why this job was queued. Defaults to <see cref="JobIntent.Conversation"/>
    /// for the existing chat / edit flow; plan verification and plan-step
    /// execution use the other values so the worker and chat history can
    /// pick out their results.
    /// </summary>
    public JobIntent Intent { get; init; } = JobIntent.Conversation;

    /// <summary>The plan this job is acting on, set when <see cref="Intent"/> is plan-related.</summary>
    public Guid? PlanId { get; init; }

    /// <summary>The plan step this job is executing, set when <see cref="Intent"/> is <see cref="JobIntent.PlanStep"/>.</summary>
    public Guid? PlanStepId { get; init; }

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
