namespace ProjectHub.Domain.Models;

/// <summary>
/// Request body for <c>POST /api/projects</c>.
/// </summary>
/// <param name="Name">Human-readable name for the project.</param>
/// <param name="ClientId">The client this project belongs to.</param>
/// <param name="RepoId">
/// Optional pointer to one of the client's registered repos. When supplied,
/// the project's working directory is taken from the repo's path. Either
/// <paramref name="RepoId"/> or <paramref name="WorkingDirectory"/> must be set.
/// </param>
/// <param name="WorkingDirectory">
/// Absolute filesystem path on the API host that Claude Code should run from
/// for every message in this project. Optional when <paramref name="RepoId"/>
/// is supplied; required otherwise.
/// </param>
/// <param name="Description">Free-form description of the project's scope.</param>
/// <param name="TicketId">Optional primary ticket for the project.</param>
public sealed record CreateProjectRequest(
    string Name,
    Guid ClientId,
    Guid? RepoId,
    string? WorkingDirectory,
    string? Description,
    Guid? TicketId);
