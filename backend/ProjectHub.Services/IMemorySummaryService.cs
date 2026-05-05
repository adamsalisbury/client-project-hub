using ProjectHub.Domain.Models;

namespace ProjectHub.Services;

/// <summary>
/// Generates AI-produced summaries of one section of a project's memory
/// (client knowledge, project knowledge, tickets, conversation history,
/// project description). The result is persisted on the project's memory
/// selection so the user can opt into using it instead of the full set.
/// </summary>
public interface IMemorySummaryService
{
    /// <summary>
    /// Asks the AI to summarise a section, persists the result on the
    /// project, and returns it.
    /// </summary>
    /// <param name="projectId">Owning project.</param>
    /// <param name="section">
    /// Section name. One of <c>clientKnowledge</c>, <c>projectKnowledge</c>,
    /// <c>tickets</c>, <c>conversation</c>, <c>projectDescription</c>.
    /// </param>
    Task<MemorySectionSummary> GenerateAsync(Guid projectId, string section, CancellationToken cancellationToken);
}
