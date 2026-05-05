using ProjectHub.Domain.Models;
using ProjectHub.Services.Storage;

namespace ProjectHub.Services;

/// <inheritdoc/>
public sealed class ClaudeJobService(
    IClaudeDataProvider data,
    IClaudeJobQueue queue,
    ILogger<ClaudeJobService> logger) : IClaudeJobService
{
    /// <inheritdoc/>
    public async Task<ClaudeJob> SubmitAsync(Guid projectId, string message, MessageKind kind, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ValidationException("The 'message' field is required.");
        }

        var project = await data.GetProjectAsync(projectId, cancellationToken)
            ?? throw new NotFoundException($"No project found with id {projectId}.");

        var job = await data.CreateJobAsync(project.Id, message, kind, cancellationToken);
        await queue.EnqueueAsync(job.Id, cancellationToken);
        logger.LogInformation("Queued {Kind} job {JobId} in project {ProjectId}", kind, job.Id, project.Id);
        return job;
    }

    /// <inheritdoc/>
    public async Task<ClaudeJob> GetAsync(Guid jobId, CancellationToken cancellationToken)
        => await data.GetJobAsync(jobId, cancellationToken)
           ?? throw new NotFoundException($"No job found with id {jobId}.");
}
