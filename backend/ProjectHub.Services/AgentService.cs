using System.Text;
using ProjectHub.Domain.Models;
using ProjectHub.Services.Runner;
using ProjectHub.Services.Storage;

namespace ProjectHub.Services;

/// <inheritdoc/>
public sealed class AgentService(
    IClaudeDataProvider data,
    IClaudeRunner runner,
    ILogger<AgentService> logger) : IAgentService
{
    private const int MaxTitleLength = 200;
    private const int MaxBodyBytes = 1 * 1024 * 1024;

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Agent>> ListAsync(Guid projectId, CancellationToken cancellationToken)
    {
        await EnsureProjectAsync(projectId, cancellationToken);
        return await data.ListAgentsByProjectAsync(projectId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Agent> GetAsync(Guid projectId, Guid agentId, CancellationToken cancellationToken)
    {
        var agent = await data.GetAgentAsync(agentId, cancellationToken);
        if (agent is null || agent.ProjectId != projectId)
        {
            throw new NotFoundException($"No agent found with id {agentId}.");
        }
        return agent;
    }

    /// <inheritdoc/>
    public async Task<Agent> CreateAsync(Guid projectId, string title, string characteristics, CancellationToken cancellationToken)
    {
        ValidateAgentInput(title, characteristics);
        await EnsureProjectAsync(projectId, cancellationToken);

        var agent = await data.CreateAgentAsync(projectId, title, characteristics, cancellationToken);
        logger.LogInformation("Created agent {AgentId} '{Title}' in project {ProjectId}", agent.Id, agent.Title, projectId);
        return agent;
    }

    /// <inheritdoc/>
    public async Task<Agent> UpdateAsync(Guid projectId, Guid agentId, string title, string characteristics, CancellationToken cancellationToken)
    {
        ValidateAgentInput(title, characteristics);
        var existing = await data.GetAgentAsync(agentId, cancellationToken);
        if (existing is null || existing.ProjectId != projectId)
        {
            throw new NotFoundException($"No agent found with id {agentId}.");
        }
        var updated = await data.UpdateAgentAsync(agentId, title, characteristics, cancellationToken)
            ?? throw new NotFoundException($"No agent found with id {agentId}.");
        return updated;
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid projectId, Guid agentId, CancellationToken cancellationToken)
    {
        var existing = await data.GetAgentAsync(agentId, cancellationToken);
        if (existing is null || existing.ProjectId != projectId)
        {
            throw new NotFoundException($"No agent found with id {agentId}.");
        }
        await data.DeleteAgentAsync(agentId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<(string Title, string Characteristics)> GenerateAsync(Guid projectId, string title, string personality, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new ValidationException("title is required.");
        if (string.IsNullOrWhiteSpace(personality)) throw new ValidationException("personality is required.");

        var project = await EnsureProjectAsync(projectId, cancellationToken);

        var prompt = BuildGenerationPrompt(title, personality);
        logger.LogInformation("Generating agent characteristics for '{Title}' in project {ProjectId}", title, projectId);

        var result = await runner.RunAsync(prompt, project.WorkingDirectory, MessageKind.Chat, cancellationToken);
        if (result.ExitCode != 0)
        {
            logger.LogWarning("Claude exited with {ExitCode} during agent generation", result.ExitCode);
            throw new UnprocessableException("Claude Code failed to generate the agent characteristics.", result.Error);
        }

        return (title.Trim(), result.Response.Trim());
    }

    private async Task<ClaudeProject> EnsureProjectAsync(Guid projectId, CancellationToken cancellationToken)
        => await data.GetProjectAsync(projectId, cancellationToken)
           ?? throw new NotFoundException($"No project found with id {projectId}.");

    private static void ValidateAgentInput(string title, string characteristics)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new ValidationException("title is required.");
        if (characteristics is null) throw new ValidationException("characteristics are required.");
        if (title.Length > MaxTitleLength) throw new ValidationException($"title must be {MaxTitleLength} characters or fewer.");
        if (Encoding.UTF8.GetByteCount(characteristics) > MaxBodyBytes)
        {
            throw new ValidationException("characteristics body exceeds the 1 MB limit.");
        }
    }

    private static string BuildGenerationPrompt(string title, string personality)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are helping define an AI agent persona that will later prefix prompts to a coding assistant.");
        sb.AppendLine();
        sb.Append("Agent title: ").AppendLine(title);
        sb.AppendLine("Personality / focus:");
        sb.AppendLine(personality);
        sb.AppendLine();
        sb.AppendLine("Reply with a markdown body describing the agent's characteristics, skills, voice, and");
        sb.AppendLine("any constraints they should keep in mind. Use clear headings (e.g. '## Skills',");
        sb.AppendLine("'## Voice', '## Constraints'). Do NOT wrap the response in code fences. Do NOT include");
        sb.AppendLine("any preamble like 'Here is...' return only the markdown body.");
        return sb.ToString();
    }
}
