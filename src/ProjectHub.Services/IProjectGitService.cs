using ProjectHub.Domain.Models;

namespace ProjectHub.Services;

/// <summary>Git operations scoped to a project's working directory.</summary>
public interface IProjectGitService
{
    Task<FileDiffResponse> GetDiffAsync(Guid projectId, string path, CancellationToken cancellationToken);
    Task<FileHistoryResponse> GetHistoryAsync(Guid projectId, string path, CancellationToken cancellationToken);
}
