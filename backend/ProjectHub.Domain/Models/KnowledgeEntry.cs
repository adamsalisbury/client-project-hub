namespace ProjectHub.Domain.Models;

/// <summary>
/// A piece of long-form context (typically markdown) attached to either a
/// project or a client. Exactly one of <see cref="ProjectId"/> /
/// <see cref="ClientId"/> must be set.
/// </summary>
public sealed class KnowledgeEntry
{
    public required Guid Id { get; init; }

    /// <summary>The project this entry belongs to (mutually exclusive with <see cref="ClientId"/>).</summary>
    public Guid? ProjectId { get; init; }

    /// <summary>The client this entry belongs to (mutually exclusive with <see cref="ProjectId"/>).</summary>
    public Guid? ClientId { get; init; }

    /// <summary>Short, human-readable label shown in lists and on tabs.</summary>
    public required string Title { get; set; }

    /// <summary>Free-form body, typically markdown.</summary>
    public required string Body { get; set; }

    public required DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? UpdatedAt { get; set; }
}
