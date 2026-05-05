using ProjectHub.Domain.Models;

namespace ProjectHub.Persistence;

/// <summary>
/// Root JSON document persisted by <see cref="JsonClaudeDataProvider"/>.
/// </summary>
internal sealed class ClaudeDataSnapshot
{
    public Dictionary<Guid, ClaudeProject> Projects { get; init; } = new();

    public Dictionary<Guid, ClaudeJob> Jobs { get; init; } = new();

    public Dictionary<Guid, Ticket> Tickets { get; init; } = new();

    public Dictionary<Guid, KnowledgeEntry> Knowledge { get; init; } = new();

    public Dictionary<Guid, ProjectClient> Clients { get; init; } = new();

    public Dictionary<Guid, KnowledgeEntry> ClientKnowledge { get; init; } = new();

    public Dictionary<Guid, Agent> Agents { get; init; } = new();
}
