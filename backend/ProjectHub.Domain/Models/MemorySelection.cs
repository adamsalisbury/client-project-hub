namespace ProjectHub.Domain.Models;

/// <summary>
/// Per-project switches that decide which pieces of context get folded into
/// every AI prompt for the project. Items are referenced by id and stored as
/// <i>exclusion</i> lists - a brand-new project (or fresh entry) is included
/// by default. May also carry a generated summary per section that the user
/// can substitute for the full set of items.
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

    /// <summary>
    /// Optional per-section AI-generated summaries the user can opt in to
    /// alongside or in place of the raw items. The dictionary key is the
    /// section name (one of <c>clientKnowledge</c>, <c>projectKnowledge</c>,
    /// <c>tickets</c>, <c>conversation</c>, <c>projectDescription</c>).
    /// </summary>
    public Dictionary<string, MemorySectionSummary> SectionSummaries { get; set; } = new();
}

/// <summary>An AI-generated compression of one memory section.</summary>
public sealed class MemorySectionSummary
{
    /// <summary>Markdown body produced by the AI.</summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>When the summary was generated.</summary>
    public DateTimeOffset GeneratedAt { get; set; }

    /// <summary>Whether the summary should be included in prompts. Defaults to false.</summary>
    public bool Included { get; set; }
}

/// <summary>Wire format for memory selection.</summary>
public sealed record MemorySelectionResponse(
    bool IncludeProjectInfo,
    bool IncludeClientInfo,
    IReadOnlyList<Guid> ExcludedAgentIds,
    IReadOnlyList<Guid> ExcludedTicketIds,
    IReadOnlyList<Guid> ExcludedProjectKnowledgeIds,
    IReadOnlyList<Guid> ExcludedClientKnowledgeIds,
    IReadOnlyList<Guid> ExcludedConversationJobIds,
    IReadOnlyDictionary<string, MemorySectionSummaryResponse> SectionSummaries)
{
    public static MemorySelectionResponse FromSelection(MemorySelection s) => new(
        s.IncludeProjectInfo,
        s.IncludeClientInfo,
        s.ExcludedAgentIds.ToArray(),
        s.ExcludedTicketIds.ToArray(),
        s.ExcludedProjectKnowledgeIds.ToArray(),
        s.ExcludedClientKnowledgeIds.ToArray(),
        s.ExcludedConversationJobIds.ToArray(),
        s.SectionSummaries.ToDictionary(
            kvp => kvp.Key,
            kvp => new MemorySectionSummaryResponse(kvp.Value.Body, kvp.Value.GeneratedAt, kvp.Value.Included)));
}

/// <summary>Wire format for a single section summary.</summary>
public sealed record MemorySectionSummaryResponse(string Body, DateTimeOffset GeneratedAt, bool Included);

/// <summary>Request body for <c>PUT /api/projects/{id}/memory-selection</c>.</summary>
public sealed record UpdateMemorySelectionRequest(
    bool IncludeProjectInfo,
    bool IncludeClientInfo,
    IReadOnlyList<Guid>? ExcludedAgentIds,
    IReadOnlyList<Guid>? ExcludedTicketIds,
    IReadOnlyList<Guid>? ExcludedProjectKnowledgeIds,
    IReadOnlyList<Guid>? ExcludedClientKnowledgeIds,
    IReadOnlyList<Guid>? ExcludedConversationJobIds,
    IReadOnlyDictionary<string, MemorySectionSummaryRequest>? SectionSummaries);

/// <summary>Request body fragment for a single section summary.</summary>
public sealed record MemorySectionSummaryRequest(string Body, DateTimeOffset GeneratedAt, bool Included);
