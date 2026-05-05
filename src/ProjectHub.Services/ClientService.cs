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
        logger.LogInformation("Created client {ClientId} '{Name}'", client.Id, client.Name);
        return client;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ClaudeProject>> ListProjectsAsync(Guid clientId, CancellationToken cancellationToken)
    {
        // Ensures the client exists; throws NotFoundException otherwise.
        await GetAsync(clientId, cancellationToken);
        return await data.ListProjectsByClientAsync(clientId, cancellationToken);
    }
}
