namespace ProjectHub.Domain.Models;

/// <summary>
/// Request body for both <c>POST /api/projects/{id}/knowledge</c> and
/// <c>POST /api/clients/{id}/knowledge</c>.
/// </summary>
public sealed record CreateKnowledgeRequest(string Title, string Body);

/// <summary>
/// Wire format for a knowledge entry. Either <see cref="ProjectId"/> or
/// <see cref="ClientId"/> is set; never both.
/// </summary>
public sealed record KnowledgeResponse(
    Guid Id,
    Guid? ProjectId,
    Guid? ClientId,
    string Title,
    string Body,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt)
{
    public static KnowledgeResponse FromEntry(KnowledgeEntry entry) => new(
        entry.Id,
        entry.ProjectId,
        entry.ClientId,
        entry.Title,
        entry.Body,
        entry.CreatedAt,
        entry.UpdatedAt);
}

/// <summary>
/// Compact form returned from list endpoints - bodies are excluded so the
/// caller can render a list cheaply, then fetch a single entry to view it.
/// </summary>
public sealed record KnowledgeSummary(
    Guid Id,
    Guid? ProjectId,
    Guid? ClientId,
    string Title,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt)
{
    public static KnowledgeSummary FromEntry(KnowledgeEntry entry) => new(
        entry.Id,
        entry.ProjectId,
        entry.ClientId,
        entry.Title,
        entry.CreatedAt,
        entry.UpdatedAt);
}
