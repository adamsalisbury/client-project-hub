using ProjectHub.Domain.Models;

namespace ProjectHub.Services;

/// <summary>Clients (top-level grouping that owns a set of projects).</summary>
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
}
