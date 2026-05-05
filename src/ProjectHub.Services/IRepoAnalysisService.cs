using ProjectHub.Domain.Models;

namespace ProjectHub.Services;

/// <summary>Runs Claude over a project's working directory and stores the result as knowledge.</summary>
public interface IRepoAnalysisService
{
    /// <summary>
    /// Asks Claude to map repo boundaries, summarise architecture per
    /// section, and write an overall summary, then saves the markdown as a
    /// knowledge entry on either the project or its owning client.
    /// </summary>
    /// <returns>The freshly created knowledge entry.</returns>
    Task<KnowledgeEntry> AnalyseAsync(Guid projectId, RepoAnalysisTarget target, CancellationToken cancellationToken);
}
