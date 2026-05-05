namespace ProjectHub.Domain.Models;

/// <summary>
/// A ticket attached to a project. Bodies are markdown text. Tickets live
/// in the data store, not on disk.
/// </summary>
public sealed class Ticket
{
    public required Guid Id { get; init; }

    public required Guid ProjectId { get; init; }

    /// <summary>Short identifier shown in the UI, e.g. "PROJ-123".</summary>
    public required string Code { get; set; }

    public required string Title { get; set; }

    /// <summary>Markdown body.</summary>
    public required string Body { get; set; }

    public required DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? UpdatedAt { get; set; }
}
