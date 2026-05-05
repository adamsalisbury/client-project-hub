using ProjectHub.Domain.Models;
using ProjectHub.Services.Runner;
using ProjectHub.Services.Storage;

namespace ProjectHub.Services.Workers;

/// <summary>
/// Background service that drains <see cref="IClaudeJobQueue"/>, runs each
/// job through the Claude Code CLI (with the full project history as context)
/// and persists the result via <see cref="IClaudeDataProvider"/>.
/// </summary>
public sealed class ClaudeJobWorker(
    IClaudeJobQueue queue,
    IClaudeDataProvider dataProvider,
    IClaudeRunner runner,
    ILogger<ClaudeJobWorker> logger) : BackgroundService
{
    private readonly FileChangeDetector _fileChangeDetector = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RecoverInterruptedJobsAsync(stoppingToken);

        logger.LogInformation("Claude job worker started; awaiting jobs.");

        await foreach (var jobId in queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessAsync(jobId, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                logger.LogInformation("Worker shutting down while processing {JobId}", jobId);
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled exception while processing job {JobId}", jobId);
                await TryMarkFailedAsync(jobId, ex.Message, stoppingToken);
            }
        }
    }

    private async Task ProcessAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var job = await dataProvider.GetJobAsync(jobId, cancellationToken);
        if (job is null)
        {
            logger.LogWarning("Queue contained id {JobId} but no matching job exists", jobId);
            return;
        }

        if (job.Status is JobStatus.Completed or JobStatus.Failed)
        {
            logger.LogDebug("Skipping {JobId}; already in terminal state {Status}", jobId, job.Status);
            return;
        }

        job.Status = JobStatus.Processing;
        job.StartedAt = DateTimeOffset.UtcNow;
        await dataProvider.UpdateJobAsync(job, cancellationToken);

        var project = await dataProvider.GetProjectAsync(job.ProjectId, cancellationToken);
        if (project is null)
        {
            logger.LogError("Job {JobId} references unknown project {ProjectId}; marking failed", jobId, job.ProjectId);
            job.Status = JobStatus.Failed;
            job.Error = $"Project {job.ProjectId} no longer exists.";
            job.CompletedAt = DateTimeOffset.UtcNow;
            await dataProvider.UpdateJobAsync(job, cancellationToken);
            return;
        }

        var priorTurns = await BuildPriorTurnsAsync(job, project, cancellationToken);
        var context = await BuildPromptContextAsync(project, cancellationToken);
        var prompt = ProjectPromptBuilder.Build(priorTurns, job.Message, context, job.Kind);

        // Capture the rendered prompt on the job before we hand it to the
        // CLI, so the diagnostics view can show exactly what was sent even
        // after agents/tickets/knowledge entries change later.
        job.Prompt = prompt;
        await dataProvider.UpdateJobAsync(job, cancellationToken);

        logger.LogInformation(
            "Processing {Kind} job {JobId} in project {ProjectId} (cwd: {WorkingDirectory}) with {TurnCount} prior turn(s)",
            job.Kind,
            jobId,
            job.ProjectId,
            project.WorkingDirectory,
            priorTurns.Count);

        var beforeSnapshot = _fileChangeDetector.Snapshot(project.WorkingDirectory);
        ClaudeResponse? result = null;
        Exception? failure = null;

        try
        {
            result = await runner.RunAsync(prompt, project.WorkingDirectory, job.Kind, cancellationToken);
        }
        catch (TimeoutException ex)
        {
            failure = ex;
            logger.LogWarning(ex, "Job {JobId} timed out", jobId);
        }
        catch (InvalidOperationException ex)
        {
            failure = ex;
            logger.LogError(ex, "Job {JobId} failed during CLI invocation", jobId);
        }

        var afterSnapshot = _fileChangeDetector.Snapshot(project.WorkingDirectory);
        job.FilesChanged = _fileChangeDetector.Diff(beforeSnapshot, afterSnapshot);
        job.CompletedAt = DateTimeOffset.UtcNow;

        if (result is not null)
        {
            job.Response = result.Response;
            job.ExitCode = result.ExitCode;
            job.DurationMs = result.DurationMs;
            job.Error = result.Error;
            job.Status = result.ExitCode == 0 ? JobStatus.Completed : JobStatus.Failed;
        }
        else if (failure is not null)
        {
            job.Status = JobStatus.Failed;
            job.Error = failure.Message;
        }

        await dataProvider.UpdateJobAsync(job, cancellationToken);

        await HandlePlanLifecycleAsync(job, cancellationToken);

        logger.LogInformation(
            "Finished {Kind} job {JobId} (intent {Intent}) with status {Status} in {DurationMs}ms; {FileCount} file(s) changed",
            job.Kind,
            jobId,
            job.Intent,
            job.Status,
            job.DurationMs,
            job.FilesChanged.Count);
    }

    private async Task HandlePlanLifecycleAsync(ClaudeJob job, CancellationToken cancellationToken)
    {
        switch (job.Intent)
        {
            case JobIntent.PlanVerification when job.PlanId is { } planId:
                if (job.Status == JobStatus.Completed && !string.IsNullOrEmpty(job.Response))
                {
                    await dataProvider.RecordPlanVerificationAsync(planId, job.Response, cancellationToken);
                    logger.LogInformation("Recorded plan verification on plan {PlanId} from job {JobId}", planId, job.Id);
                }
                break;

            case JobIntent.PlanStep when job.PlanStepId is { } stepId:
                if (job.Status == JobStatus.Completed)
                {
                    var review = await dataProvider.CreateStepReviewAsync(
                        job.ProjectId,
                        stepId,
                        job.Id,
                        job.FilesChanged,
                        cancellationToken);
                    await dataProvider.UpdatePlanStepStatusAsync(
                        stepId,
                        PlanStepStatus.AwaitingReview,
                        completedAt: DateTimeOffset.UtcNow,
                        cancellationToken: cancellationToken);
                    logger.LogInformation(
                        "Created step review {ReviewId} with {FileCount} file(s) for step {StepId}",
                        review.Id, job.FilesChanged.Count, stepId);
                }
                else
                {
                    await dataProvider.UpdatePlanStepStatusAsync(
                        stepId,
                        PlanStepStatus.Failed,
                        completedAt: DateTimeOffset.UtcNow,
                        cancellationToken: cancellationToken);
                }
                break;
        }
    }

    private async Task<PromptContext> BuildPromptContextAsync(ClaudeProject project, CancellationToken cancellationToken)
    {
        var selection = project.MemorySelection;
        var excludedTickets = selection.ExcludedTicketIds.ToHashSet();
        var excludedProjectKnowledge = selection.ExcludedProjectKnowledgeIds.ToHashSet();
        var excludedClientKnowledge = selection.ExcludedClientKnowledgeIds.ToHashSet();
        var excludedAgents = selection.ExcludedAgentIds.ToHashSet();

        var tickets = (await dataProvider.ListTicketsByProjectAsync(project.Id, cancellationToken))
            .Where(t => !excludedTickets.Contains(t.Id))
            .ToList();
        var projectKnowledge = (await dataProvider.ListKnowledgeByProjectAsync(project.Id, cancellationToken))
            .Where(k => !excludedProjectKnowledge.Contains(k.Id))
            .ToList();
        var agents = (await dataProvider.ListAgentsByProjectAsync(project.Id, cancellationToken))
            .Where(a => !excludedAgents.Contains(a.Id))
            .ToList();

        string? clientName = null;
        IReadOnlyList<KnowledgeEntry> clientKnowledge = Array.Empty<KnowledgeEntry>();
        var client = await dataProvider.GetClientAsync(project.ClientId, cancellationToken);
        if (client is not null)
        {
            clientName = client.Name;
            clientKnowledge = (await dataProvider.ListClientKnowledgeAsync(client.Id, cancellationToken))
                .Where(k => !excludedClientKnowledge.Contains(k.Id))
                .ToList();
        }

        return new PromptContext(
            IncludeProjectInfo: selection.IncludeProjectInfo,
            ProjectName: project.Name,
            WorkingDirectory: project.WorkingDirectory,
            IncludeClientInfo: selection.IncludeClientInfo,
            ClientName: clientName,
            ClientKnowledge: clientKnowledge,
            ProjectKnowledge: projectKnowledge,
            Tickets: tickets,
            Agents: agents);
    }

    private async Task<IReadOnlyList<ConversationTurn>> BuildPriorTurnsAsync(ClaudeJob currentJob, ClaudeProject project, CancellationToken cancellationToken)
    {
        var projectJobs = await dataProvider.ListJobsByProjectAsync(currentJob.ProjectId, cancellationToken);
        var excludedJobIds = project.MemorySelection.ExcludedConversationJobIds.ToHashSet();

        return projectJobs
            .Where(j => j.Id != currentJob.Id)
            .Where(j => !excludedJobIds.Contains(j.Id))
            .Where(j => j.CreatedAt < currentJob.CreatedAt)
            .Where(j => j.Status == JobStatus.Completed && !string.IsNullOrEmpty(j.Response))
            .OrderBy(j => j.CreatedAt)
            .Select(j => new ConversationTurn(j.Message, j.Response!))
            .ToList();
    }

    private async Task RecoverInterruptedJobsAsync(CancellationToken cancellationToken)
    {
        var interrupted = await dataProvider.ListJobsByStatusAsync(JobStatus.Processing, cancellationToken);
        foreach (var job in interrupted)
        {
            logger.LogWarning("Resetting in-flight job {JobId} back to Queued after restart", job.Id);
            job.Status = JobStatus.Queued;
            job.StartedAt = null;
            await dataProvider.UpdateJobAsync(job, cancellationToken);
        }

        var queued = await dataProvider.ListJobsByStatusAsync(JobStatus.Queued, cancellationToken);
        foreach (var job in queued.OrderBy(j => j.CreatedAt))
        {
            await queue.EnqueueAsync(job.Id, cancellationToken);
        }

        if (queued.Count > 0 || interrupted.Count > 0)
        {
            logger.LogInformation(
                "Re-enqueued {QueuedCount} queued and {InterruptedCount} interrupted jobs from the store.",
                queued.Count,
                interrupted.Count);
        }
    }

    private async Task TryMarkFailedAsync(Guid jobId, string error, CancellationToken cancellationToken)
    {
        try
        {
            var job = await dataProvider.GetJobAsync(jobId, cancellationToken);
            if (job is null)
            {
                return;
            }

            job.Status = JobStatus.Failed;
            job.Error = error;
            job.CompletedAt = DateTimeOffset.UtcNow;
            await dataProvider.UpdateJobAsync(job, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not mark job {JobId} as failed", jobId);
        }
    }
}
