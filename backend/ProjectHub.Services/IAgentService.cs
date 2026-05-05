using ProjectHub.Domain.Models;

namespace ProjectHub.Services;

/// <summary>Lifecycle for project-level agent personae.</summary>
public interface IAgentService
{
    Task<IReadOnlyList<Agent>> ListAsync(Guid projectId, CancellationToken cancellationToken);
    Task<Agent> GetAsync(Guid projectId, Guid agentId, CancellationToken cancellationToken);
    Task<Agent> CreateAsync(Guid projectId, string title, string characteristics, CancellationToken cancellationToken);
    Task<Agent> UpdateAsync(Guid projectId, Guid agentId, string title, string characteristics, CancellationToken cancellationToken);
    Task DeleteAsync(Guid projectId, Guid agentId, CancellationToken cancellationToken);

    /// <summary>
    /// Asks the runner to expand a personality blurb into a full
    /// characteristics body. The result is not persisted.
    /// </summary>
    Task<(string Title, string Characteristics)> GenerateAsync(Guid projectId, string title, string personality, CancellationToken cancellationToken);
}
