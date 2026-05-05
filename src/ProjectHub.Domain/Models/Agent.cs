namespace ProjectHub.Domain.Models;

/// <summary>
/// A persona that prefixes Claude's prompt with a "You are a …" preamble when
/// included in a project's memory selection. Each agent has a short title
/// (e.g. "Senior .NET reviewer") and a longer free-form characteristics body
/// (typically markdown).
/// </summary>
public sealed class Agent
{
    public required Guid Id { get; init; }

    public required Guid ProjectId { get; init; }

    public required string Title { get; set; }

    /// <summary>Free-form characteristics / skills text, typically markdown.</summary>
    public required string Characteristics { get; set; }

    public required DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? UpdatedAt { get; set; }
}
