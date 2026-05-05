namespace ProjectHub.Domain.Models;

/// <summary>
/// Wire format describing a project.
/// </summary>
public sealed record ClaudeProjectResponse(
    Guid Id,
    string Name,
    string? WorkingDirectory,
    DateTimeOffset CreatedAt,
    Guid ClientId,
    Guid? RepoId,
    string? Description,
    Guid? TicketId)
{
    public static ClaudeProjectResponse FromProject(ClaudeProject project)
        => new(project.Id, project.Name, project.WorkingDirectory, project.CreatedAt,
               project.ClientId, project.RepoId, project.Description, project.TicketId);
}
