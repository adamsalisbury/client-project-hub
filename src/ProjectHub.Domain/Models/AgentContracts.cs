namespace ProjectHub.Domain.Models;

/// <summary>Request body for <c>POST /api/projects/{id}/agents</c>.</summary>
public sealed record CreateAgentRequest(string Title, string Characteristics);

/// <summary>Request body for <c>POST /api/projects/{id}/agents/generate</c>.</summary>
public sealed record GenerateAgentRequest(string Title, string Personality);

/// <summary>Wire format for an agent.</summary>
public sealed record AgentResponse(
    Guid Id,
    Guid ProjectId,
    string Title,
    string Characteristics,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt)
{
    public static AgentResponse FromAgent(Agent agent) => new(
        agent.Id,
        agent.ProjectId,
        agent.Title,
        agent.Characteristics,
        agent.CreatedAt,
        agent.UpdatedAt);
}

/// <summary>Compact form returned from list endpoints (no body).</summary>
public sealed record AgentSummary(
    Guid Id,
    Guid ProjectId,
    string Title,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt)
{
    public static AgentSummary FromAgent(Agent agent) => new(
        agent.Id,
        agent.ProjectId,
        agent.Title,
        agent.CreatedAt,
        agent.UpdatedAt);
}

/// <summary>Response from the agent-characteristics generator.</summary>
public sealed record GeneratedAgentResponse(string Title, string Characteristics);
