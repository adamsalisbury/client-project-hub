using ProjectHub.Domain.Models;

namespace ProjectHub.Services;

/// <summary>Read-only browse + read against a project's working directory.</summary>
public interface IProjectFileService
{
    Task<ProjectFileListing> ListAsync(Guid projectId, string? path, CancellationToken cancellationToken);
    Task<ProjectFileContent> ReadAsync(Guid projectId, string path, CancellationToken cancellationToken);
}
