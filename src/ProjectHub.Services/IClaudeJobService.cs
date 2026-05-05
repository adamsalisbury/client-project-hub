using ProjectHub.Domain.Models;

namespace ProjectHub.Services;

/// <summary>Submission and status retrieval for Claude conversation jobs.</summary>
public interface IClaudeJobService
{
    Task<ClaudeJob> SubmitAsync(Guid projectId, string message, MessageKind kind, CancellationToken cancellationToken);
    Task<ClaudeJob> GetAsync(Guid jobId, CancellationToken cancellationToken);
}
