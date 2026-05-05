using System.Diagnostics;
using System.Globalization;
using System.Text;
using ProjectHub.Domain.Models;
using ProjectHub.Services.Storage;

namespace ProjectHub.Services;

/// <inheritdoc/>
public sealed class ProjectGitService(IClaudeDataProvider data, ILogger<ProjectGitService> logger) : IProjectGitService
{
    private const int HistoryLimit = 50;
    private const int GitTimeoutMs = 10_000;

    /// <inheritdoc/>
    public async Task<FileDiffResponse> GetDiffAsync(Guid projectId, string path, CancellationToken cancellationToken)
    {
        var (project, relative, resolved) = await ResolveAsync(projectId, path, cancellationToken);

        var statusResult = await RunGitAsync(
            project.RequireWorkingDirectory(),
            ["status", "--porcelain", "--", relative],
            cancellationToken);

        if (statusResult.ExitCode != 0)
        {
            logger.LogWarning("git status failed in {Cwd}: {Stderr}", project.RequireWorkingDirectory(), statusResult.StdErr);
            throw new UnprocessableException("git status failed.", statusResult.StdErr);
        }

        var status = statusResult.StdOut.Trim();
        var isUntracked = status.StartsWith("??", StringComparison.Ordinal);

        if (isUntracked)
        {
            string content;
            try
            {
                content = await File.ReadAllTextAsync(resolved, Encoding.UTF8, cancellationToken);
            }
            catch (IOException ex)
            {
                logger.LogWarning(ex, "Failed to read untracked file {Path}", resolved);
                throw new UnprocessableException(ex.Message);
            }
            var synthesised = BuildAddedFileDiff(relative, content);
            return new FileDiffResponse(relative, HasChanges: true, IsUntracked: true, Diff: synthesised);
        }

        var diffResult = await RunGitAsync(
            project.RequireWorkingDirectory(),
            ["diff", "HEAD", "--no-color", "--", relative],
            cancellationToken);

        if (diffResult.ExitCode != 0)
        {
            logger.LogWarning("git diff failed in {Cwd}: {Stderr}", project.RequireWorkingDirectory(), diffResult.StdErr);
            throw new UnprocessableException("git diff failed.", diffResult.StdErr);
        }

        var hasChanges = !string.IsNullOrWhiteSpace(diffResult.StdOut);
        return new FileDiffResponse(relative, hasChanges, IsUntracked: false, Diff: diffResult.StdOut);
    }

    /// <inheritdoc/>
    public async Task<FileHistoryResponse> GetHistoryAsync(Guid projectId, string path, CancellationToken cancellationToken)
    {
        var (project, relative, _) = await ResolveAsync(projectId, path, cancellationToken);

        const string Format = "%H%x09%h%x09%an%x09%ae%x09%aI%x09%s%x1f";

        var logResult = await RunGitAsync(
            project.RequireWorkingDirectory(),
            ["log", $"-n{HistoryLimit}", $"--pretty=format:{Format}", "--", relative],
            cancellationToken);

        if (logResult.ExitCode != 0)
        {
            logger.LogWarning("git log failed in {Cwd}: {Stderr}", project.RequireWorkingDirectory(), logResult.StdErr);
            throw new UnprocessableException("git log failed.", logResult.StdErr);
        }

        var commits = new List<FileCommitEntry>();
        foreach (var record in logResult.StdOut.Split('\u001f', StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = record.Trim('\r', '\n');
            if (trimmed.Length == 0)
            {
                continue;
            }
            var parts = trimmed.Split('\t');
            if (parts.Length < 6)
            {
                continue;
            }
            if (!DateTimeOffset.TryParse(parts[4], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var date))
            {
                date = DateTimeOffset.MinValue;
            }
            commits.Add(new FileCommitEntry(
                Sha: parts[0],
                ShortSha: parts[1],
                Author: parts[2],
                Email: parts[3],
                Date: date,
                Subject: parts[5]));
        }

        return new FileHistoryResponse(relative, commits);
    }

    private async Task<(ClaudeProject Project, string Relative, string Resolved)> ResolveAsync(Guid projectId, string path, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ValidationException("The 'path' query parameter is required.");
        }

        var project = await data.GetProjectAsync(projectId, cancellationToken)
            ?? throw new NotFoundException($"No project found with id {projectId}.");

        if (!ProjectPathResolver.TryResolve(project.RequireWorkingDirectory(), path, out var resolved, out var error))
        {
            throw new ValidationException(error);
        }
        var relative = ProjectPathResolver.ToRelative(project.RequireWorkingDirectory(), resolved);
        if (string.IsNullOrEmpty(relative))
        {
            throw new ValidationException("Cannot operate on the project root.");
        }

        return (project, relative, resolved);
    }

    private static string BuildAddedFileDiff(string relative, string content)
    {
        var sb = new StringBuilder();
        sb.Append("diff --git a/").Append(relative).Append(" b/").AppendLine(relative);
        sb.AppendLine("new file mode 100644");
        sb.AppendLine("--- /dev/null");
        sb.Append("+++ b/").AppendLine(relative);

        var lines = content.Split('\n');
        var lineCount = lines.Length;
        if (lineCount > 0 && lines[^1].Length == 0)
        {
            lineCount--;
        }
        sb.Append("@@ -0,0 +1,").Append(lineCount).AppendLine(" @@");
        for (var i = 0; i < lineCount; i++)
        {
            sb.Append('+').AppendLine(lines[i].TrimEnd('\r'));
        }
        return sb.ToString();
    }

    private async Task<GitInvocation> RunGitAsync(string workingDirectory, string[] args, CancellationToken cancellationToken)
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

        var stdout = await stdoutTask;
        var stderr = await stderrTask;
        return new GitInvocation(process.ExitCode, stdout, stderr);
    }

    private readonly record struct GitInvocation(int ExitCode, string StdOut, string StdErr);
}
