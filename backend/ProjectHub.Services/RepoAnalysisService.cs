using System.Text;
using ProjectHub.Domain.Models;
using ProjectHub.Services.Runner;
using ProjectHub.Services.Storage;

namespace ProjectHub.Services;

/// <inheritdoc/>
public sealed class RepoAnalysisService(
    IClaudeDataProvider data,
    IClaudeRunner runner,
    ILogger<RepoAnalysisService> logger) : IRepoAnalysisService
{
    /// <inheritdoc/>
    public async Task<KnowledgeEntry> AnalyseAsync(Guid projectId, RepoAnalysisTarget target, CancellationToken cancellationToken)
    {
        var project = await data.GetProjectAsync(projectId, cancellationToken)
            ?? throw new NotFoundException($"No project found with id {projectId}.");

        ProjectClient? client = null;
        if (target == RepoAnalysisTarget.Client)
        {
            client = await data.GetClientAsync(project.ClientId, cancellationToken)
                ?? throw new NotFoundException($"No client found with id {project.ClientId}.");
        }

        var prompt = BuildPrompt(project, client);
        logger.LogInformation(
            "Running repo analysis for project {ProjectId} (target: {Target})",
            projectId, target);

        var result = await runner.RunAsync(prompt, project.WorkingDirectory, MessageKind.Chat, cancellationToken);
        if (result.ExitCode != 0)
        {
            logger.LogWarning("Claude exited with {ExitCode} during repo analysis", result.ExitCode);
            throw new UnprocessableException("Claude Code failed to analyse the repository.", result.Error);
        }

        var body = result.Response.Trim();
        if (string.IsNullOrEmpty(body))
        {
            throw new UnprocessableException("Claude returned an empty analysis.");
        }

        var title = BuildTitle(project, target);

        return target == RepoAnalysisTarget.Client
            ? await data.CreateClientKnowledgeAsync(client!.Id, title, body, cancellationToken)
            : await data.CreateKnowledgeAsync(project.Id, title, body, cancellationToken);
    }

    private static string BuildTitle(ClaudeProject project, RepoAnalysisTarget target)
    {
        var stamp = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd");
        return target == RepoAnalysisTarget.Client
            ? $"Repo analysis: {project.Name} ({stamp})"
            : $"Repo analysis ({stamp})";
    }

    private static string BuildPrompt(ClaudeProject project, ProjectClient? client)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are auditing a code repository so that future prompts have a structured");
        sb.AppendLine("write-up of what is here. Use the Read, Grep and Glob tools to explore the");
        sb.AppendLine("working directory; do not modify any files.");
        sb.AppendLine();

        if (client is not null)
        {
            sb.Append("This analysis will be saved on the client '").Append(client.Name).AppendLine("',");
            sb.AppendLine("which may cover several related repositories. Open with a clear identifier so");
            sb.AppendLine("readers know which repo this section is about:");
            sb.AppendLine();
            sb.Append("- Repo name: ").AppendLine(project.Name);
            sb.Append("- Working directory: ").AppendLine(project.WorkingDirectory);
            sb.AppendLine();
        }
        else
        {
            sb.AppendLine("This analysis will be saved as a knowledge entry on the current project.");
            sb.AppendLine();
        }

        sb.AppendLine("Produce a single markdown document with these sections, in order:");
        sb.AppendLine();
        sb.AppendLine("## Summary");
        sb.AppendLine("A short paragraph (3 to 6 sentences) on what this repository does overall: the");
        sb.AppendLine("problem it solves, the high-level shape of it, and the runtime model.");
        sb.AppendLine();
        sb.AppendLine("## Sections");
        sb.AppendLine("Identify each distinct section / boundary inside the repo (e.g. JS frontend, C#");
        sb.AppendLine("Web API, background workers, hosted services, persistence layer, infrastructure");
        sb.AppendLine("scripts). For each one use a `### <section name>` heading and cover, in concise");
        sb.AppendLine("bullets:");
        sb.AppendLine();
        sb.AppendLine("- Path(s) it lives in");
        sb.AppendLine("- Language / framework / key libraries");
        sb.AppendLine("- Purpose: what it owns or contributes");
        sb.AppendLine("- Architecture & design choices: layering, patterns, notable trade-offs");
        sb.AppendLine();
        sb.AppendLine("## How the sections fit together");
        sb.AppendLine("A short paragraph or diagram-in-text describing the runtime / build-time");
        sb.AppendLine("relationships between the sections (e.g. \"the API hosts the SPA and dispatches");
        sb.AppendLine("jobs to a background worker that shells out to ...\").");
        sb.AppendLine();
        sb.AppendLine("Stay grounded in what the code actually shows; avoid invention. Do NOT wrap the");
        sb.AppendLine("response in code fences. Do NOT include any preamble like 'Here is...'; return");
        sb.AppendLine("only the markdown body.");

        return sb.ToString();
    }
}
