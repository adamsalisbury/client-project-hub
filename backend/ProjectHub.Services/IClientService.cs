using ProjectHub.Domain.Models;

namespace ProjectHub.Services;

/// <summary>Clients (top-level grouping that owns a set of projects, repos, knowledge, and a tab colour).</summary>
public interface IClientService
{
    /// <summary>Lists every client, oldest first.</summary>
    Task<IReadOnlyList<ProjectClient>> ListAsync(CancellationToken cancellationToken);

    /// <summary>Retrieves a client by id, throwing <see cref="NotFoundException"/> if missing.</summary>
    Task<ProjectClient> GetAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Creates a new client with the supplied name.</summary>
    Task<ProjectClient> CreateAsync(string name, CancellationToken cancellationToken);

    /// <summary>Lists every project under a client, oldest first.</summary>
    Task<IReadOnlyList<ClaudeProject>> ListProjectsAsync(Guid clientId, CancellationToken cancellationToken);

    /// <summary>Sets a client's tab colour. Validates that the value is a <c>#RRGGBB</c> hex string.</summary>
    Task<ProjectClient> UpdateColourAsync(Guid id, string colour, CancellationToken cancellationToken);

    /// <summary>Lists every repo registered against a client.</summary>
    Task<IReadOnlyList<ClientRepo>> ListReposAsync(Guid clientId, CancellationToken cancellationToken);

    /// <summary>
    /// Registers a new repo against a client. Validates that the path exists
    /// on the API host and is a git working tree.
    /// </summary>
    Task<ClientRepo> AddRepoAsync(Guid clientId, string name, string path, CancellationToken cancellationToken);

    /// <summary>Removes a repo. Projects that pointed at it are detached but remain.</summary>
    Task RemoveRepoAsync(Guid repoId, CancellationToken cancellationToken);
}
