using System.Text;
using ProjectHub.Domain.Models;
using ProjectHub.Services.Runner;
using ProjectHub.Services.Storage;

namespace ProjectHub.Services;

/// <inheritdoc/>
public sealed class MemorySummaryService(
    IClaudeDataProvider data,
    IClaudeRunner runner,
    ILogger<MemorySummaryService> logger) : IMemorySummaryService
{
    private static readonly HashSet<string> s_validSections = new(StringComparer.Ordinal)
    {
        "clientKnowledge",
        "projectKnowledge",
        "tickets",
        "conversation",
        "projectDescription"
    };

    /// <inheritdoc/>
    public async Task<MemorySectionSummary> GenerateAsync(Guid projectId, string section, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(section) || !s_validSections.Contains(section))
        {
            throw new ValidationException($"Unknown memory section '{section}'.");
        }

        var project = await data.GetProjectAsync(projectId, cancellationToken)
            ?? throw new NotFoundException($"No project found with id {projectId}.");

        var sourceText = await BuildSourceTextAsync(project, section, cancellationToken);
        if (string.IsNullOrWhiteSpace(sourceText))
        {
            throw new ValidationException($"Section '{section}' has no content to summarise.");
        }

        var prompt = BuildSummaryPrompt(section, sourceText);
        logger.LogInformation("Summarising section '{Section}' for project {ProjectId}", section, projectId);

        var workingDirectory = project.WorkingDirectory ?? Path.GetTempPath();
        var result = await runner.RunAsync(prompt, workingDirectory, MessageKind.Chat, cancellationToken);
        if (result.ExitCode != 0)
        {
            throw new UnprocessableException("AI failed to generate the summary.", result.Error);
        }

        var body = result.Response.Trim();
        if (string.IsNullOrEmpty(body))
        {
            throw new UnprocessableException("AI returned an empty summary.");
        }

        var summary = new MemorySectionSummary
        {
            Body = body,
            GeneratedAt = DateTimeOffset.UtcNow,
            Included = false
        };

        var nextSelection = CloneSelection(project.MemorySelection);
        nextSelection.SectionSummaries[section] = summary;
        await data.UpdateMemorySelectionAsync(projectId, nextSelection, cancellationToken);

        return summary;
    }

    private async Task<string> BuildSourceTextAsync(ClaudeProject project, string section, CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();
        switch (section)
        {
            case "clientKnowledge":
            {
                if (project.ClientId == Guid.Empty) break;
                var entries = await data.ListClientKnowledgeAsync(project.ClientId, cancellationToken);
                foreach (var entry in entries)
                {
                    sb.Append("### ").AppendLine(entry.Title);
                    sb.AppendLine(entry.Body);
                    sb.AppendLine();
                }
                break;
            }
            case "projectKnowledge":
            {
                var entries = await data.ListKnowledgeByProjectAsync(project.Id, cancellationToken);
                foreach (var entry in entries)
                {
                    sb.Append("### ").AppendLine(entry.Title);
                    sb.AppendLine(entry.Body);
                    sb.AppendLine();
                }
                break;
            }
            case "tickets":
            {
                var tickets = await data.ListTicketsByProjectAsync(project.Id, cancellationToken);
                foreach (var ticket in tickets)
                {
                    sb.Append("Ticket ").Append(ticket.Code).Append(": ").AppendLine(ticket.Title);
                    sb.AppendLine(ticket.Body);
                    sb.AppendLine();
                }
                break;
            }
            case "conversation":
            {
                var jobs = await data.ListJobsByProjectAsync(project.Id, cancellationToken);
                foreach (var job in jobs.Where(j => j.Status == JobStatus.Completed && !string.IsNullOrEmpty(j.Response)))
                {
                    sb.Append("User: ").AppendLine(job.Message);
                    sb.Append("Assistant: ").AppendLine(job.Response);
                    sb.AppendLine();
                }
                break;
            }
            case "projectDescription":
            {
                if (!string.IsNullOrWhiteSpace(project.Description))
                {
                    sb.AppendLine(project.Description);
                }
                break;
            }
        }
        return sb.ToString();
    }

    private static string BuildSummaryPrompt(string section, string sourceText)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are compressing a block of project context for re-use in future AI prompts.");
        sb.Append("The block is the project's '").Append(SectionLabel(section)).AppendLine("'.");
        sb.AppendLine();
        sb.AppendLine("Produce a succinct, non-verbose summary aimed at being read by a language model,");
        sb.AppendLine("not a human. Cut filler, keep concrete facts, IDs, names, paths, and conclusions.");
        sb.AppendLine("Do NOT wrap the response in code fences and do NOT include any preamble.");
        sb.AppendLine();
        sb.AppendLine("--- source ---");
        sb.AppendLine(sourceText);
        sb.AppendLine("--- end of source ---");
        return sb.ToString();
    }

    private static string SectionLabel(string section) => section switch
    {
        "clientKnowledge" => "client knowledge",
        "projectKnowledge" => "project knowledge",
        "tickets" => "tickets",
        "conversation" => "conversation history",
        "projectDescription" => "project description",
        _ => section
    };

    private static MemorySelection CloneSelection(MemorySelection selection) => new()
    {
        IncludeProjectInfo = selection.IncludeProjectInfo,
        IncludeClientInfo = selection.IncludeClientInfo,
        ExcludedAgentIds = new List<Guid>(selection.ExcludedAgentIds),
        ExcludedTicketIds = new List<Guid>(selection.ExcludedTicketIds),
        ExcludedProjectKnowledgeIds = new List<Guid>(selection.ExcludedProjectKnowledgeIds),
        ExcludedClientKnowledgeIds = new List<Guid>(selection.ExcludedClientKnowledgeIds),
        ExcludedConversationJobIds = new List<Guid>(selection.ExcludedConversationJobIds),
        SectionSummaries = selection.SectionSummaries.ToDictionary(
            kvp => kvp.Key,
            kvp => new MemorySectionSummary
            {
                Body = kvp.Value.Body,
                GeneratedAt = kvp.Value.GeneratedAt,
                Included = kvp.Value.Included
            })
    };
}
