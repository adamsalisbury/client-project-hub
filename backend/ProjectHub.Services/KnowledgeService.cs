using System.Text;
using ProjectHub.Domain.Models;
using ProjectHub.Services.Storage;

namespace ProjectHub.Services;

/// <inheritdoc/>
public sealed class KnowledgeService(IClaudeDataProvider data, ILogger<KnowledgeService> logger) : IKnowledgeService
{
    private const int MaxTitleLength = 200;
    private const int MaxBodyBytes = 1 * 1024 * 1024;

    /// <inheritdoc/>
    public async Task<IReadOnlyList<KnowledgeEntry>> ListProjectKnowledgeAsync(Guid projectId, CancellationToken cancellationToken)
    {
        await EnsureProjectAsync(projectId, cancellationToken);
        return await data.ListKnowledgeByProjectAsync(projectId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<KnowledgeEntry> GetProjectKnowledgeAsync(Guid projectId, Guid id, CancellationToken cancellationToken)
    {
        var entry = await data.GetKnowledgeAsync(id, cancellationToken);
        if (entry is null || entry.ProjectId != projectId)
        {
            throw new NotFoundException($"No knowledge entry found with id {id}.");
        }
        return entry;
    }

    /// <inheritdoc/>
    public async Task<KnowledgeEntry> CreateProjectKnowledgeAsync(Guid projectId, string title, string body, CancellationToken cancellationToken)
    {
        ValidateInput(title, body);
        await EnsureProjectAsync(projectId, cancellationToken);
        var entry = await data.CreateKnowledgeAsync(projectId, title, body, cancellationToken);
        logger.LogInformation("Created project knowledge {EntryId} '{Title}' in project {ProjectId}", entry.Id, entry.Title, projectId);
        return entry;
    }

    /// <inheritdoc/>
    public async Task DeleteProjectKnowledgeAsync(Guid projectId, Guid id, CancellationToken cancellationToken)
    {
        var existing = await data.GetKnowledgeAsync(id, cancellationToken);
        if (existing is null || existing.ProjectId != projectId)
        {
            throw new NotFoundException($"No knowledge entry found with id {id}.");
        }
        await data.DeleteKnowledgeAsync(id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<KnowledgeEntry>> ListClientKnowledgeAsync(Guid clientId, CancellationToken cancellationToken)
    {
        await EnsureClientAsync(clientId, cancellationToken);
        return await data.ListClientKnowledgeAsync(clientId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<KnowledgeEntry> GetClientKnowledgeAsync(Guid clientId, Guid id, CancellationToken cancellationToken)
    {
        var entry = await data.GetClientKnowledgeAsync(id, cancellationToken);
        if (entry is null || entry.ClientId != clientId)
        {
            throw new NotFoundException($"No knowledge entry found with id {id}.");
        }
        return entry;
    }

    /// <inheritdoc/>
    public async Task<KnowledgeEntry> CreateClientKnowledgeAsync(Guid clientId, string title, string body, CancellationToken cancellationToken)
    {
        ValidateInput(title, body);
        await EnsureClientAsync(clientId, cancellationToken);
        var entry = await data.CreateClientKnowledgeAsync(clientId, title, body, cancellationToken);
        logger.LogInformation("Created client knowledge {EntryId} '{Title}' on client {ClientId}", entry.Id, entry.Title, clientId);
        return entry;
    }

    /// <inheritdoc/>
    public async Task DeleteClientKnowledgeAsync(Guid clientId, Guid id, CancellationToken cancellationToken)
    {
        var entry = await data.GetClientKnowledgeAsync(id, cancellationToken);
        if (entry is null || entry.ClientId != clientId)
        {
            throw new NotFoundException($"No knowledge entry found with id {id}.");
        }
        await data.DeleteClientKnowledgeAsync(id, cancellationToken);
    }

    private async Task EnsureProjectAsync(Guid projectId, CancellationToken cancellationToken)
    {
        if (await data.GetProjectAsync(projectId, cancellationToken) is null)
        {
            throw new NotFoundException($"No project found with id {projectId}.");
        }
    }

    private async Task EnsureClientAsync(Guid clientId, CancellationToken cancellationToken)
    {
        if (await data.GetClientAsync(clientId, cancellationToken) is null)
        {
            throw new NotFoundException($"No client found with id {clientId}.");
        }
    }

    private static void ValidateInput(string title, string body)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new ValidationException("title is required.");
        if (body is null) throw new ValidationException("body is required.");
        if (title.Length > MaxTitleLength) throw new ValidationException($"Title must be {MaxTitleLength} characters or fewer.");
        if (Encoding.UTF8.GetByteCount(body) > MaxBodyBytes)
        {
            throw new ValidationException("Body exceeds the 1 MB limit.");
        }
    }
}
