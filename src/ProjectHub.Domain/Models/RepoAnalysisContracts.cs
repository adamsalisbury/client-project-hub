namespace ProjectHub.Domain.Models;

/// <summary>Where the analysis result should be saved.</summary>
public enum RepoAnalysisTarget
{
    Project,
    Client
}

/// <summary>Request body for <c>POST /api/projects/{id}/analyse-repo</c>.</summary>
public sealed record AnalyseRepoRequest(RepoAnalysisTarget Target);

/// <summary>
/// Response from <c>POST /api/projects/{id}/analyse-repo</c>: returns the
/// freshly created knowledge entry holding the markdown analysis.
/// </summary>
public sealed record RepoAnalysisResponse(KnowledgeResponse Knowledge, RepoAnalysisTarget Target);
