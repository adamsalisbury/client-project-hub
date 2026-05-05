namespace ProjectHub.Domain.Models;

/// <summary>
/// A code repository registered against a client. Projects under that client
/// pick one of these repos as their working directory; repos may be added at
/// any time and reused across multiple projects.
/// </summary>
public sealed class ClientRepo
{
    public required Guid Id { get; init; }

    public required Guid ClientId { get; init; }

    /// <summary>Short, human-readable label used in pickers and tab titles.</summary>
    public required string Name { get; set; }

    /// <summary>Absolute filesystem path on the API host.</summary>
    public required string Path { get; set; }

    public required DateTimeOffset CreatedAt { get; init; }
}
