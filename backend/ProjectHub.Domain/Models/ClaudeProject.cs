namespace ProjectHub.Domain.Models;

/// <summary>
/// A grouping of related AI jobs whose full message history is
/// passed back to the AI on each new request. Every project belongs to a
/// client and may be linked to one of that client's registered repos. The
/// working directory is derived from that repo; a project with no repo has
/// no working directory and cannot run code-touching operations.
/// </summary>
public sealed class ClaudeProject
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    /// <summary>
    /// Absolute filesystem path used as the working directory for every AI
    /// invocation in this project. Mirrors the path of <see cref="RepoId"/>
    /// when one is assigned; <see langword="null"/> when the project has no
    /// repo. Operations requiring a working directory (chat, files, plan)
    /// fail with a clear error in that case.
    /// </summary>
    public string? WorkingDirectory { get; set; }

    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// The client the project is grouped under. The client's knowledge
    /// entries are folded into Claude's prompt context for this project.
    /// Validated at the service layer when the project is created;
    /// orphaned projects loaded from older data files are adopted by a
    /// "Default" client at startup.
    /// </summary>
    public Guid ClientId { get; set; }

    /// <summary>
    /// Optional pointer to a <see cref="ClientRepo"/> registered against
    /// the project's client. May be left null at creation and set later.
    /// When set, <see cref="WorkingDirectory"/> tracks the repo's path.
    /// </summary>
    public Guid? RepoId { get; set; }

    /// <summary>
    /// Free-form description of the work this project covers (e.g. the
    /// user story summary). Surfaced on the project tab.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Optional pointer to a single ticket the project is delivering.
    /// Tickets continue to live independently under <c>Tickets</c>; this
    /// field designates the "primary" one shown on the project view.
    /// </summary>
    public Guid? TicketId { get; set; }

    /// <summary>
    /// Per-project switches deciding which pieces of context are sent to
    /// Claude with each prompt.
    /// </summary>
    public MemorySelection MemorySelection { get; set; } = new();
}
