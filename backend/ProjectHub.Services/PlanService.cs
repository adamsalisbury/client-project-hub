using System.Text;
using ProjectHub.Domain.Models;
using ProjectHub.Services.Storage;

namespace ProjectHub.Services;

/// <inheritdoc/>
public sealed class PlanService(
    IClaudeDataProvider data,
    IClaudeJobQueue queue,
    ILogger<PlanService> logger) : IPlanService
{
    private const int MaxTitleLength = 200;

    /// <inheritdoc/>
    public async Task<(Plan Plan, IReadOnlyList<PlanStep> Steps)> GetAsync(Guid projectId, CancellationToken cancellationToken)
    {
        await EnsureProjectAsync(projectId, cancellationToken);
        var plan = await data.GetOrCreatePlanAsync(projectId, cancellationToken);
        var steps = await data.ListPlanStepsAsync(plan.Id, cancellationToken);
        return (plan, steps);
    }

    /// <inheritdoc/>
    public async Task<PlanStep> AddStepAsync(Guid projectId, string title, string? description, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ValidationException("title is required.");
        }
        if (title.Length > MaxTitleLength)
        {
            throw new ValidationException($"title must be {MaxTitleLength} characters or fewer.");
        }
        var (plan, _) = await GetAsync(projectId, cancellationToken);
        return await data.AddPlanStepAsync(plan.Id, title, description ?? string.Empty, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<PlanStep> UpdateStepAsync(Guid projectId, Guid stepId, string title, string? description, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ValidationException("title is required.");
        }
        await EnsureProjectAsync(projectId, cancellationToken);
        var step = await data.UpdatePlanStepAsync(stepId, title, description ?? string.Empty, cancellationToken)
            ?? throw new NotFoundException($"No plan step found with id {stepId}.");
        return step;
    }

    /// <inheritdoc/>
    public async Task RemoveStepAsync(Guid projectId, Guid stepId, CancellationToken cancellationToken)
    {
        await EnsureProjectAsync(projectId, cancellationToken);
        if (!await data.DeletePlanStepAsync(stepId, cancellationToken))
        {
            throw new NotFoundException($"No plan step found with id {stepId}.");
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<PlanStep>> ReorderAsync(Guid projectId, IReadOnlyList<Guid> orderedStepIds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(orderedStepIds);
        var (plan, _) = await GetAsync(projectId, cancellationToken);
        try
        {
            return await data.ReorderPlanStepsAsync(plan.Id, orderedStepIds, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            throw new ValidationException(ex.Message);
        }
    }

    /// <inheritdoc/>
    public async Task<ClaudeJob> VerifyAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var (plan, steps) = await GetAsync(projectId, cancellationToken);
        if (steps.Count == 0)
        {
            throw new ValidationException("Cannot verify an empty plan.");
        }

        var prompt = BuildVerificationPrompt(steps);
        var job = await data.CreateJobAsync(
            projectId,
            prompt,
            MessageKind.Chat,
            JobIntent.PlanVerification,
            plan.Id,
            cancellationToken: cancellationToken);
        await queue.EnqueueAsync(job.Id, cancellationToken);
        logger.LogInformation("Queued plan-verify job {JobId} for plan {PlanId}", job.Id, plan.Id);
        return job;
    }

    /// <inheritdoc/>
    public async Task<ClaudeJob> ExecuteStepAsync(Guid projectId, Guid stepId, CancellationToken cancellationToken)
    {
        var (plan, steps) = await GetAsync(projectId, cancellationToken);
        var step = steps.FirstOrDefault(s => s.Id == stepId)
            ?? throw new NotFoundException($"No plan step found with id {stepId}.");

        var index = steps.ToList().FindIndex(s => s.Id == stepId);
        var preceding = steps.Take(index).ToList();

        var prompt = BuildStepPrompt(step, preceding);
        var job = await data.CreateJobAsync(
            projectId,
            prompt,
            MessageKind.Edit,
            JobIntent.PlanStep,
            plan.Id,
            step.Id,
            cancellationToken);

        await data.UpdatePlanStepStatusAsync(
            stepId,
            PlanStepStatus.Running,
            jobId: job.Id,
            startedAt: DateTimeOffset.UtcNow,
            cancellationToken: cancellationToken);

        await queue.EnqueueAsync(job.Id, cancellationToken);
        logger.LogInformation("Queued plan-step job {JobId} for step {StepId} (plan {PlanId})", job.Id, step.Id, plan.Id);
        return job;
    }

    private async Task EnsureProjectAsync(Guid projectId, CancellationToken cancellationToken)
    {
        if (await data.GetProjectAsync(projectId, cancellationToken) is null)
        {
            throw new NotFoundException($"No project found with id {projectId}.");
        }
    }

    private static string BuildVerificationPrompt(IReadOnlyList<PlanStep> steps)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are reviewing a development plan for an upcoming piece of work.");
        sb.AppendLine("Read every step in order and assess: are the steps in a sensible order? Are there");
        sb.AppendLine("missing steps? Will any step block the next? Be specific and concise.");
        sb.AppendLine();
        sb.AppendLine("# Plan");
        for (var i = 0; i < steps.Count; i++)
        {
            var step = steps[i];
            sb.Append("## Step ").Append(i + 1).Append(": ").AppendLine(step.Title);
            if (!string.IsNullOrWhiteSpace(step.Description))
            {
                sb.AppendLine(step.Description);
            }
            sb.AppendLine();
        }
        sb.AppendLine("Return your opinion as plain markdown.");
        return sb.ToString();
    }

    private static string BuildStepPrompt(PlanStep step, IReadOnlyList<PlanStep> preceding)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are executing a single step of a multi-step development plan.");
        if (preceding.Count > 0)
        {
            sb.AppendLine("Earlier steps that have already been completed (do not redo them):");
            for (var i = 0; i < preceding.Count; i++)
            {
                sb.Append("- Step ").Append(i + 1).Append(": ").AppendLine(preceding[i].Title);
            }
            sb.AppendLine();
        }
        sb.Append("Current step (").Append("Step ").Append(preceding.Count + 1).Append("): ").AppendLine(step.Title);
        if (!string.IsNullOrWhiteSpace(step.Description))
        {
            sb.AppendLine();
            sb.AppendLine(step.Description);
        }
        sb.AppendLine();
        sb.AppendLine("Make exactly the changes this step requires; do not start the next step.");
        return sb.ToString();
    }
}
