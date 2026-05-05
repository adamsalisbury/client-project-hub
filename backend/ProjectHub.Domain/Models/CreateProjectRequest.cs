namespace ProjectHub.Domain.Models;

/// <summary>
/// Request body for <c>POST /api/projects</c>.
/// </summary>
/// <param name="Name">Human-readable name for the project.</param>
/// <param name="WorkingDirectory">
/// Absolute filesystem path on the API host that Claude Code should run from
/// for every message in this project.
/// </param>
/// <param name="ClientId">The client this project belongs to.</param>
public sealed record CreateProjectRequest(string Name, string WorkingDirectory, Guid ClientId);
