using System.Diagnostics;
using System.Text;
using ProjectHub.Domain.Models;
using ProjectHub.Services.Storage;

namespace ProjectHub.Services;

/// <inheritdoc/>
public sealed class StepReviewService(IClaudeDataProvider data, ILogger<StepReviewService> logger) : IStepReviewService
{
    private const int GitTimeoutMs = 15_000;

    /// <inheritdoc/>
    public async Task<IReadOnlyList<StepReview>> ListByProjectAsync(Guid projectId, CancellationToken cancellationToken)
    {
        if (await data.GetProjectAsync(projectId, cancellationToken) is null)
        {
            throw new NotFoundException($"No project found with id {projectId}.");
        }
        return await data.ListStepReviewsByProjectAsync(projectId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<StepReview> GetAsync(Guid reviewId, CancellationToken cancellationToken)
        => await data.GetStepReviewAsync(reviewId, cancellationToken)
           ?? throw new NotFoundException($"No step review found with id {reviewId}.");

    /// <inheritdoc/>
    public async Task<StepReview> CommitFileAsync(Guid reviewId, string path, string? message, CancellationToken cancellationToken)
    {
        var (review, project, file, relative) = await ResolveAsync(reviewId, path, cancellationToken);
        if (file.State != StepReviewFileState.Pending)
        {
            throw new ValidationException($"File '{relative}' has already been resolved.");
        }

        var commitMessage = string.IsNullOrWhiteSpace(message)
            ? $"step {review.StepId} — {relative}"
            : message;

        var addResult = await RunGitAsync(project.WorkingDirectory, ["add", "--", relative], cancellationToken);
        if (addResult.ExitCode != 0)
        {
            throw new UnprocessableException("git add failed.", addResult.StdErr);
        }

        var commitResult = await RunGitAsync(
            project.WorkingDirectory,
            ["commit", "--only", "--", relative, "-m", commitMessage],
            cancellationToken);
        if (commitResult.ExitCode != 0)
        {
            throw new UnprocessableException("git commit failed.", commitResult.StdErr);
        }

        var updated = await data.UpdateStepReviewFileAsync(reviewId, file.Path, StepReviewFileState.Committed, cancellationToken)
            ?? throw new NotFoundException($"Review {reviewId} disappeared during update.");
        logger.LogInformation("Committed {Path} for step review {ReviewId}", relative, reviewId);
        return updated;
    }

    /// <inheritdoc/>
    public async Task<StepReview> RollbackFileAsync(Guid reviewId, string path, CancellationToken cancellationToken)
    {
        var (review, project, file, relative) = await ResolveAsync(reviewId, path, cancellationToken);
        if (file.State != StepReviewFileState.Pending)
        {
            throw new ValidationException($"File '{relative}' has already been resolved.");
        }

        var status = await RunGitAsync(project.WorkingDirectory, ["status", "--porcelain", "--", relative], cancellationToken);
        if (status.ExitCode != 0)
        {
            throw new UnprocessableException("git status failed.", status.StdErr);
        }

        var line = status.StdOut.Trim();
        if (line.StartsWith("??", StringComparison.Ordinal))
        {
            // Untracked: just delete the file.
            var absolute = Path.Combine(project.WorkingDirectory, relative);
            if (File.Exists(absolute))
            {
                File.Delete(absolute);
            }
        }
        else
        {
            var checkout = await RunGitAsync(project.WorkingDirectory, ["checkout", "HEAD", "--", relative], cancellationToken);
            if (checkout.ExitCode != 0)
            {
                throw new UnprocessableException("git checkout failed.", checkout.StdErr);
            }
        }

        var updated = await data.UpdateStepReviewFileAsync(reviewId, file.Path, StepReviewFileState.RolledBack, cancellationToken)
            ?? throw new NotFoundException($"Review {reviewId} disappeared during update.");
        logger.LogInformation("Rolled back {Path} for step review {ReviewId}", relative, reviewId);
        return updated;
    }

    private async Task<(StepReview Review, ClaudeProject Project, StepReviewFile File, string Relative)>
        ResolveAsync(Guid reviewId, string path, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ValidationException("path is required.");
        }
        var review = await GetAsync(reviewId, cancellationToken);
        var project = await data.GetProjectAsync(review.ProjectId, cancellationToken)
            ?? throw new NotFoundException($"Project {review.ProjectId} for review {reviewId} no longer exists.");

        if (!ProjectPathResolver.TryResolve(project.WorkingDirectory, path, out var resolved, out var error))
        {
            throw new ValidationException(error);
        }
        var relative = ProjectPathResolver.ToRelative(project.WorkingDirectory, resolved);
        if (string.IsNullOrEmpty(relative))
        {
            throw new ValidationException("Cannot operate on the project root.");
        }

        var file = review.Files.FirstOrDefault(f => string.Equals(f.Path, relative, StringComparison.Ordinal))
            ?? throw new NotFoundException($"Review {reviewId} does not include file '{relative}'.");

        return (review, project, file, relative);
    }

    private static async Task<GitInvocation> RunGitAsync(string workingDirectory, string[] args, CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo("git")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };
        foreach (var arg in args)
        {
            psi.ArgumentList.Add(arg);
        }

        using var process = new Process { StartInfo = psi };
        process.Start();

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(GitTimeoutMs);

        var stdoutTask = process.StandardOutput.ReadToEndAsync(timeoutCts.Token);
        var stderrTask = process.StandardError.ReadToEndAsync(timeoutCts.Token);

        try
        {
            await process.WaitForExitAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(entireProcessTree: true); } catch { /* best effort */ }
            throw;
        }

        return new GitInvocation(process.ExitCode, await stdoutTask, await stderrTask);
    }

    private readonly record struct GitInvocation(int ExitCode, string StdOut, string StdErr);
}
