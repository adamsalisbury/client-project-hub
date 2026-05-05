namespace ProjectHub.Domain.Models;

/// <summary>
/// Per-project switches that decide which pieces of context get folded into
/// every Claude prompt for the project. Items are referenced by id and
/// stored as <i>exclusion</i> lists - a brand-new project (or fresh entry)
/// is included by default.
/// </summary>
public sealed class MemorySelection
{
    /// <summary>When false, the project name + working directory preamble is omitted.</summary>
    public bool IncludeProjectInfo { get; set; } = true;

    /// <summary>When false, the client name preamble is omitted (client knowledge is still gated below).</summary>
    public bool IncludeClientInfo { get; set; } = true;

    public List<Guid> ExcludedAgentIds { get; set; } = new();
    public List<Guid> ExcludedTicketIds { get; set; } = new();
    public List<Guid> ExcludedProjectKnowledgeIds { get; set; } = new();
    public List<Guid> ExcludedClientKnowledgeIds { get; set; } = new();

    /// <summary>Conversation turns are referenced by their job id.</summary>
    public List<Guid> ExcludedConversationJobIds { get; set; } = new();
}

/// <summary>Wire format for memory selection.</summary>
public sealed record MemorySelectionResponse(
    bool IncludeProjectInfo,
    bool IncludeClientInfo,
    IReadOnlyList<Guid> ExcludedAgentIds,
    IReadOnlyList<Guid> ExcludedTicketIds,
    IReadOnlyList<Guid> ExcludedProjectKnowledgeIds,
    IReadOnlyList<Guid> ExcludedClientKnowledgeIds,
    IReadOnlyList<Guid> ExcludedConversationJobIds)
{
    public static MemorySelectionResponse FromSelection(MemorySelection s) => new(
        s.IncludeProjectInfo,
        s.IncludeClientInfo,
        s.ExcludedAgentIds.ToArray(),
        s.ExcludedTicketIds.ToArray(),
        s.ExcludedProjectKnowledgeIds.ToArray(),
        s.ExcludedClientKnowledgeIds.ToArray(),
        s.ExcludedConversationJobIds.ToArray());
}

/// <summary>Request body for <c>PUT /api/projects/{id}/memory-selection</c>.</summary>
public sealed record UpdateMemorySelectionRequest(
    bool IncludeProjectInfo,
    bool IncludeClientInfo,
    IReadOnlyList<Guid>? ExcludedAgentIds,
    IReadOnlyList<Guid>? ExcludedTicketIds,
    IReadOnlyList<Guid>? ExcludedProjectKnowledgeIds,
    IReadOnlyList<Guid>? ExcludedClientKnowledgeIds,
    IReadOnlyList<Guid>? ExcludedConversationJobIds);
