using ProjectHub.Domain.Models;
using ProjectHub.Services.Storage;

namespace ProjectHub.Services;

/// <inheritdoc/>
public sealed class ClientService(IClaudeDataProvider data, ILogger<ClientService> logger) : IClientService
{
    private const int MaxNameLength = 200;

    /// <inheritdoc/>
    public Task<IReadOnlyList<ProjectClient>> ListAsync(CancellationToken cancellationToken)
        => data.ListClientsAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<ProjectClient> GetAsync(Guid id, CancellationToken cancellationToken)
        => await data.GetClientAsync(id, cancellationToken)
           ?? throw new NotFoundException($"No client found with id {id}.");

    /// <inheritdoc/>
    public async Task<ProjectClient> CreateAsync(string name, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationException("name is required.");
        }
        if (name.Length > MaxNameLength)
        {
            throw new ValidationException($"name must be {MaxNameLength} characters or fewer.");
        }

        var client = await data.CreateClientAsync(name, cancellationToken);
        logger.LogInformation("Created client {ClientId} '{Name}' colour {Colour}", client.Id, client.Name, client.Colour);
        return client;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ClaudeProject>> ListProjectsAsync(Guid clientId, CancellationToken cancellationToken)
    {
        await GetAsync(clientId, cancellationToken);
        return await data.ListProjectsByClientAsync(clientId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ProjectClient> UpdateColourAsync(Guid id, string colour, CancellationToken cancellationToken)
    {
        if (!ClientColours.IsValid(colour))
        {
            throw new ValidationException("colour must be a #RRGGBB hex string.");
        }

        var updated = await data.UpdateClientColourAsync(id, colour, cancellationToken)
            ?? throw new NotFoundException($"No client found with id {id}.");
        logger.LogInformation("Client {ClientId} colour set to {Colour}", id, colour);
        return updated;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ClientRepo>> ListReposAsync(Guid clientId, CancellationToken cancellationToken)
    {
        await GetAsync(clientId, cancellationToken);
        return await data.ListClientReposAsync(clientId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ClientRepo> AddRepoAsync(Guid clientId, string name, string path, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationException("name is required.");
        }
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ValidationException("path is required.");
        }
        await GetAsync(clientId, cancellationToken);

        string resolved;
        try
        {
            resolved = Path.GetFullPath(path);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            throw new ValidationException($"'{path}' is not a valid path.");
        }
        if (!Directory.Exists(resolved))
        {
            throw new ValidationException($"Directory '{resolved}' does not exist on the API host.");
        }
        if (!Path.Exists(Path.Combine(resolved, ".git")))
        {
            throw new ValidationException(
                $"Directory '{resolved}' is not a git repository. Run 'git init' first or pick a different folder.");
        }

        try
        {
            var repo = await data.CreateClientRepoAsync(clientId, name, resolved, cancellationToken);
            logger.LogInformation("Registered repo {RepoId} '{Name}' at {Path} on client {ClientId}",
                repo.Id, repo.Name, repo.Path, clientId);
            return repo;
        }
        catch (InvalidOperationException ex)
        {
            throw new ValidationException(ex.Message);
        }
    }

    /// <inheritdoc/>
    public async Task RemoveRepoAsync(Guid repoId, CancellationToken cancellationToken)
    {
        var deleted = await data.DeleteClientRepoAsync(repoId, cancellationToken);
        if (!deleted)
        {
            throw new NotFoundException($"No repo found with id {repoId}.");
        }
        logger.LogInformation("Removed client repo {RepoId}", repoId);
    }
}
