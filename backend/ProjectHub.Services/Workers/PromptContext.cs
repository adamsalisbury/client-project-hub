using ProjectHub.Domain.Models;

namespace ProjectHub.Services.Workers;

/// <summary>
/// Bundle of project-level context that is folded into every prompt sent to
/// Claude alongside the prior conversation turns. The worker resolves and
/// filters this against the project's memory selection before passing it to
/// the prompt builder.
/// </summary>
public sealed record PromptContext(
    bool IncludeProjectInfo,
    string ProjectName,
    string WorkingDirectory,
    bool IncludeClientInfo,
    string? ClientName,
    IReadOnlyList<KnowledgeEntry> ClientKnowledge,
    IReadOnlyList<KnowledgeEntry> ProjectKnowledge,
    IReadOnlyList<Ticket> Tickets,
    IReadOnlyList<Agent> Agents,
    string? AiName = null)
{
    public static PromptContext Empty { get; } = new(
        IncludeProjectInfo: false,
        ProjectName: string.Empty,
        WorkingDirectory: string.Empty,
        IncludeClientInfo: false,
        ClientName: null,
        ClientKnowledge: Array.Empty<KnowledgeEntry>(),
        ProjectKnowledge: Array.Empty<KnowledgeEntry>(),
        Tickets: Array.Empty<Ticket>(),
        Agents: Array.Empty<Agent>(),
        AiName: null);
}
