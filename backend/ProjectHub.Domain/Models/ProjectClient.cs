namespace ProjectHub.Domain.Models;

/// <summary>
/// A client (top-of-tree grouping). Every project belongs to a client; a
/// client may carry its own knowledge entries that are inherited as context
/// by every project beneath it, and registers the repos its projects can
/// run from.
/// </summary>
public sealed class ProjectClient
{
    public required Guid Id { get; init; }

    public required string Name { get; set; }

    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Hex colour (<c>#RRGGBB</c>) used to tint this client's tab strip and
    /// the strip of every project / sub-tab nested under it. Auto-assigned
    /// from <see cref="ClientColours.Palette"/> on creation; user-editable
    /// through the colour picker on the client view.
    /// </summary>
    public string Colour { get; set; } = ClientColours.Default;
}
