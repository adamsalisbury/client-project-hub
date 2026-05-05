namespace ProjectHub.Domain.Models;

/// <summary>
/// Returned from <c>POST /api/claude</c> immediately after a job is queued.
/// </summary>
public sealed record ClaudeJobSubmittedResponse(Guid Id, Guid ProjectId, MessageKind Kind, JobStatus Status);
