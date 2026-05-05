namespace ProjectHub.Domain.Models;

/// <summary>Wire format for a client repo.</summary>
public sealed record ClientRepoResponse(
    Guid Id,
    Guid ClientId,
    string Name,
    string Path,
    DateTimeOffset CreatedAt)
{
    public static ClientRepoResponse FromRepo(ClientRepo repo)
        => new(repo.Id, repo.ClientId, repo.Name, repo.Path, repo.CreatedAt);
}

/// <summary>Request body for <c>POST /api/clients/{id}/repos</c>.</summary>
public sealed record CreateClientRepoRequest(string Name, string Path);

/// <summary>Request body for <c>PUT /api/clients/{id}/colour</c>.</summary>
public sealed record SetClientColourRequest(string Colour);

/// <summary>Request body for <c>PUT /api/projects/{id}/repo</c>.</summary>
public sealed record AssignProjectRepoRequest(Guid? RepoId);
