using ProjectHub.Domain.Models;

namespace ProjectHub.Services;

/// <summary>Project- and client-scoped knowledge entries.</summary>
public interface IKnowledgeService
{
    Task<IReadOnlyList<KnowledgeEntry>> ListProjectKnowledgeAsync(Guid projectId, CancellationToken cancellationToken);
    Task<KnowledgeEntry> GetProjectKnowledgeAsync(Guid projectId, Guid id, CancellationToken cancellationToken);
    Task<KnowledgeEntry> CreateProjectKnowledgeAsync(Guid projectId, string title, string body, CancellationToken cancellationToken);
    Task DeleteProjectKnowledgeAsync(Guid projectId, Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<KnowledgeEntry>> ListClientKnowledgeAsync(Guid clientId, CancellationToken cancellationToken);
    Task<KnowledgeEntry> GetClientKnowledgeAsync(Guid clientId, Guid id, CancellationToken cancellationToken);
    Task<KnowledgeEntry> CreateClientKnowledgeAsync(Guid clientId, string title, string body, CancellationToken cancellationToken);
    Task DeleteClientKnowledgeAsync(Guid clientId, Guid id, CancellationToken cancellationToken);
}
