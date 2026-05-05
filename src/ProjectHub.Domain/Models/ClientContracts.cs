namespace ProjectHub.Domain.Models;

/// <summary>Request body for <c>POST /api/clients</c>.</summary>
public sealed record CreateClientRequest(string Name);

/// <summary>Wire format for a client.</summary>
public sealed record ClientResponse(Guid Id, string Name, DateTimeOffset CreatedAt)
{
    public static ClientResponse FromClient(ProjectClient c) => new(c.Id, c.Name, c.CreatedAt);
}

/// <summary>Request body for <c>PUT /api/projects/{id}/client</c>.</summary>
public sealed record AssignClientRequest(Guid ClientId);
