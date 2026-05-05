namespace ProjectHub.Domain.Models;

/// <summary>
/// A client (top-of-tree grouping). Every project belongs to a client; a
/// client may carry its own knowledge entries that are inherited as context
/// by every project beneath it.
/// </summary>
public sealed class ProjectClient
{
    public required Guid Id { get; init; }

    public required string Name { get; set; }

    public required DateTimeOffset CreatedAt { get; init; }
}
