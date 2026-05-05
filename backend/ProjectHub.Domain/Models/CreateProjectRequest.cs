namespace ProjectHub.Domain.Models;

/// <summary>
/// Request body for <c>POST /api/projects</c>.
/// </summary>
/// <param name="Name">Human-readable name for the project.</param>
/// <param name="ClientId">The client this project belongs to.</param>
/// <param name="RepoId">
/// Optional pointer to one of the client's registered repos. When supplied,
/// the project's working directory is taken from the repo's path. When null
/// the project starts detached and a repo can be assigned later.
/// </param>
/// <param name="Description">Free-form description of the project's scope.</param>
/// <param name="TicketId">Optional primary ticket for the project.</param>
public sealed record CreateProjectRequest(
    string Name,
    Guid ClientId,
    Guid? RepoId,
    string? Description,
    Guid? TicketId);
