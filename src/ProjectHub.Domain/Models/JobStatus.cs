namespace ProjectHub.Domain.Models;

/// <summary>
/// Lifecycle state of a Claude Code job.
/// </summary>
public enum JobStatus
{
    /// <summary>Job has been accepted and is awaiting a worker.</summary>
    Queued,

    /// <summary>A worker has picked up the job and is invoking the CLI.</summary>
    Processing,

    /// <summary>The CLI exited with code 0 and a response was captured.</summary>
    Completed,

    /// <summary>The CLI exited non-zero, timed out, or could not be started.</summary>
    Failed
}
