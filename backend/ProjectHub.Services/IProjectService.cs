using ProjectHub.Domain.Models;

namespace ProjectHub.Services;

/// <summary>Project lifecycle, history, client attachment, and memory selection.</summary>
public interface IProjectService
{
    Task<ClaudeProject> CreateAsync(string name, string workingDirectory, Guid clientId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ClaudeProject>> ListAsync(CancellationToken cancellationToken);
    Task<ClaudeProject?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<(ClaudeProject Project, IReadOnlyList<ClaudeJob> Jobs)?> GetHistoryAsync(Guid id, CancellationToken cancellationToken);
    Task<ClaudeProject> AssignClientAsync(Guid projectId, Guid clientId, CancellationToken cancellationToken);
    Task<MemoryUsageResponse> GetMemoryUsageAsync(Guid projectId, CancellationToken cancellationToken);
    Task<MemorySelection> GetMemorySelectionAsync(Guid projectId, CancellationToken cancellationToken);
    Task<MemorySelection> UpdateMemorySelectionAsync(Guid projectId, MemorySelection selection, CancellationToken cancellationToken);
}
