namespace ProjectHub.Domain.Models;

/// <summary>
/// A grouping of related Claude Code jobs whose full message history is
/// passed back into Claude on each new request, all run from a single
/// pinned working directory. Every project belongs to a client.
/// </summary>
public sealed class ClaudeProject
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    /// <summary>
    /// Absolute filesystem path used as the working directory for every
    /// Claude Code invocation in this project.
    /// </summary>
    public required string WorkingDirectory { get; init; }

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
    /// Per-project switches deciding which pieces of context are sent to
    /// Claude with each prompt.
    /// </summary>
    public MemorySelection MemorySelection { get; set; } = new();
}
