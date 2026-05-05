using ProjectHub.Domain.Models;

namespace ProjectHub.Services;

/// <summary>Project lifecycle, history, client / repo attachment, and memory selection.</summary>
public interface IProjectService
{
    /// <summary>
    /// Creates a project. <paramref name="repoId"/> is optional; when null
    /// the project starts with no working directory and code-touching
    /// operations (chat, files, plan) are blocked until a repo is assigned
    /// via <see cref="AssignRepoAsync"/>.
    /// </summary>
    Task<ClaudeProject> CreateAsync(
        string name,
        Guid clientId,
        Guid? repoId,
        string? description,
        Guid? ticketId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ClaudeProject>> ListAsync(CancellationToken cancellationToken);
    Task<ClaudeProject?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<(ClaudeProject Project, IReadOnlyList<ClaudeJob> Jobs)?> GetHistoryAsync(Guid id, CancellationToken cancellationToken);
    Task<ClaudeProject> AssignClientAsync(Guid projectId, Guid clientId, CancellationToken cancellationToken);

    /// <summary>Links a project to one of its client's repos, or clears the link when <paramref name="repoId"/> is null.</summary>
    Task<ClaudeProject> AssignRepoAsync(Guid projectId, Guid? repoId, CancellationToken cancellationToken);

    /// <summary>Updates the editable bits of a project.</summary>
    Task<ClaudeProject> UpdateAsync(Guid projectId, string? description, Guid? ticketId, CancellationToken cancellationToken);

    Task<MemoryUsageResponse> GetMemoryUsageAsync(Guid projectId, CancellationToken cancellationToken);
    Task<MemorySelection> GetMemorySelectionAsync(Guid projectId, CancellationToken cancellationToken);
    Task<MemorySelection> UpdateMemorySelectionAsync(Guid projectId, MemorySelection selection, CancellationToken cancellationToken);
}
