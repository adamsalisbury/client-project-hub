using System.Text;
using ProjectHub.Domain.Models;
using ProjectHub.Services.Storage;

namespace ProjectHub.Services;

/// <inheritdoc/>
public sealed class ProjectService(IClaudeDataProvider data, ILogger<ProjectService> logger) : IProjectService
{
    private const long MaxMemoryBytes = 1_000_000;

    /// <inheritdoc/>
    public async Task<ClaudeProject> CreateAsync(
        string name,
        Guid clientId,
        Guid? repoId,
        string? workingDirectory,
        string? description,
        Guid? ticketId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationException("The 'name' field is required.");
        }
        if (clientId == Guid.Empty)
        {
            throw new ValidationException("The 'clientId' field is required.");
        }
        if (await data.GetClientAsync(clientId, cancellationToken) is null)
        {
            throw new ValidationException($"No client found with id {clientId}.");
        }

        string resolved;
        ClientRepo? repo = null;
        if (repoId is { } id && id != Guid.Empty)
        {
            repo = await data.GetClientRepoAsync(id, cancellationToken)
                ?? throw new ValidationException($"No repo found with id {id}.");
            if (repo.ClientId != clientId)
            {
                throw new ValidationException($"Repo {id} does not belong to client {clientId}.");
            }
            resolved = repo.Path;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(workingDirectory))
            {
                throw new ValidationException("Either 'repoId' or 'workingDirectory' must be supplied.");
            }
            try
            {
                resolved = Path.GetFullPath(workingDirectory);
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
            {
                throw new ValidationException($"'{workingDirectory}' is not a valid path.");
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
        }

        var project = await data.CreateProjectAsync(name, resolved, clientId, cancellationToken);

        // If the caller supplied a working directory rather than a repoId,
        // promote the directory to a registered repo on the client and link
        // the project to it, so the new repo-pointer model is consistent.
        if (repo is null)
        {
            var derivedName = DeriveRepoName(resolved);
            repo = await data.CreateClientRepoAsync(clientId, derivedName, resolved, cancellationToken);
        }

        await data.AssignProjectRepoAsync(project.Id, repo.Id, cancellationToken);

        if (description is not null || ticketId is not null)
        {
            project = await data.UpdateProjectAsync(project.Id, description, ticketId, cancellationToken: cancellationToken)
                ?? project;
        }
        else
        {
            project = await data.GetProjectAsync(project.Id, cancellationToken) ?? project;
        }

        logger.LogInformation(
            "Created project {ProjectId} named '{Name}' under client {ClientId} repo {RepoId} (cwd: {WorkingDirectory})",
            project.Id, project.Name, project.ClientId, project.RepoId, project.WorkingDirectory);
        return project;
    }

    /// <inheritdoc/>
    public async Task<ClaudeProject> AssignRepoAsync(Guid projectId, Guid? repoId, CancellationToken cancellationToken)
    {
        try
        {
            var project = await data.AssignProjectRepoAsync(projectId, repoId, cancellationToken);
            if (project is null)
            {
                throw new NotFoundException($"No project found with id {projectId}.");
            }
            logger.LogInformation("Project {ProjectId} repo set to {RepoId}", projectId, repoId);
            return project;
        }
        catch (InvalidOperationException ex)
        {
            throw new ValidationException(ex.Message);
        }
    }

    /// <inheritdoc/>
    public async Task<ClaudeProject> UpdateAsync(Guid projectId, string? description, Guid? ticketId, CancellationToken cancellationToken)
    {
        var project = await data.UpdateProjectAsync(projectId, description, ticketId, cancellationToken: cancellationToken)
            ?? throw new NotFoundException($"No project found with id {projectId}.");
        return project;
    }

    private static string DeriveRepoName(string workingDirectory)
    {
        var trimmed = workingDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var leaf = Path.GetFileName(trimmed);
        return string.IsNullOrEmpty(leaf) ? workingDirectory : leaf;
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<ClaudeProject>> ListAsync(CancellationToken cancellationToken)
        => data.ListProjectsAsync(cancellationToken);

    /// <inheritdoc/>
    public Task<ClaudeProject?> GetAsync(Guid id, CancellationToken cancellationToken)
        => data.GetProjectAsync(id, cancellationToken);

    /// <inheritdoc/>
    public async Task<(ClaudeProject Project, IReadOnlyList<ClaudeJob> Jobs)?> GetHistoryAsync(Guid id, CancellationToken cancellationToken)
    {
        var project = await data.GetProjectAsync(id, cancellationToken);
        if (project is null)
        {
            return null;
        }
        var jobs = await data.ListJobsByProjectAsync(id, cancellationToken);
        return (project, jobs);
    }

    /// <inheritdoc/>
    public async Task<ClaudeProject> AssignClientAsync(Guid projectId, Guid clientId, CancellationToken cancellationToken)
    {
        if (clientId == Guid.Empty)
        {
            throw new ValidationException("The 'clientId' field is required.");
        }

        try
        {
            var project = await data.AssignProjectClientAsync(projectId, clientId, cancellationToken);
            if (project is null)
            {
                throw new NotFoundException($"No project found with id {projectId}.");
            }
            logger.LogInformation(
                "Project {ProjectId} client set to {ClientId}",
                projectId, clientId);
            return project;
        }
        catch (InvalidOperationException ex)
        {
            throw new ValidationException(ex.Message);
        }
    }

    /// <inheritdoc/>
    public async Task<MemoryUsageResponse> GetMemoryUsageAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var project = await data.GetProjectAsync(projectId, cancellationToken)
            ?? throw new NotFoundException($"No project found with id {projectId}.");

        var selection = project.MemorySelection;
        var excludedJobs = selection.ExcludedConversationJobIds.ToHashSet();
        var excludedTickets = selection.ExcludedTicketIds.ToHashSet();
        var excludedProjectKnowledge = selection.ExcludedProjectKnowledgeIds.ToHashSet();
        var excludedClientKnowledge = selection.ExcludedClientKnowledgeIds.ToHashSet();
        var excludedAgents = selection.ExcludedAgentIds.ToHashSet();

        long conversationBytes = 0;
        var jobs = await data.ListJobsByProjectAsync(projectId, cancellationToken);
        foreach (var job in jobs)
        {
            if (excludedJobs.Contains(job.Id)) continue;
            conversationBytes += Encoding.UTF8.GetByteCount(job.Message);
            if (!string.IsNullOrEmpty(job.Response))
            {
                conversationBytes += Encoding.UTF8.GetByteCount(job.Response);
            }
        }

        long ticketBytes = 0;
        var tickets = await data.ListTicketsByProjectAsync(projectId, cancellationToken);
        foreach (var ticket in tickets)
        {
            if (excludedTickets.Contains(ticket.Id)) continue;
            ticketBytes += Encoding.UTF8.GetByteCount(ticket.Title);
            ticketBytes += Encoding.UTF8.GetByteCount(ticket.Body);
            ticketBytes += Encoding.UTF8.GetByteCount(ticket.Code);
        }

        long projectKnowledgeBytes = 0;
        var projectKnowledge = await data.ListKnowledgeByProjectAsync(projectId, cancellationToken);
        foreach (var entry in projectKnowledge)
        {
            if (excludedProjectKnowledge.Contains(entry.Id)) continue;
            projectKnowledgeBytes += Encoding.UTF8.GetByteCount(entry.Title);
            projectKnowledgeBytes += Encoding.UTF8.GetByteCount(entry.Body);
        }

        long clientKnowledgeBytes = 0;
        var clientKnowledge = await data.ListClientKnowledgeAsync(project.ClientId, cancellationToken);
        foreach (var entry in clientKnowledge)
        {
            if (excludedClientKnowledge.Contains(entry.Id)) continue;
            clientKnowledgeBytes += Encoding.UTF8.GetByteCount(entry.Title);
            clientKnowledgeBytes += Encoding.UTF8.GetByteCount(entry.Body);
        }

        long agentBytes = 0;
        var agents = await data.ListAgentsByProjectAsync(projectId, cancellationToken);
        foreach (var agent in agents)
        {
            if (excludedAgents.Contains(agent.Id)) continue;
            agentBytes += Encoding.UTF8.GetByteCount(agent.Title);
            agentBytes += Encoding.UTF8.GetByteCount(agent.Characteristics);
        }

        var projectInfoBytes = selection.IncludeProjectInfo
            ? Encoding.UTF8.GetByteCount(project.Name) + Encoding.UTF8.GetByteCount(project.WorkingDirectory)
            : 0L;

        var total = conversationBytes
            + ticketBytes
            + projectKnowledgeBytes
            + clientKnowledgeBytes
            + agentBytes
            + projectInfoBytes;

        return new MemoryUsageResponse(
            ConversationBytes: conversationBytes,
            TicketBytes: ticketBytes,
            ProjectKnowledgeBytes: projectKnowledgeBytes,
            ClientKnowledgeBytes: clientKnowledgeBytes,
            AgentBytes: agentBytes,
            ProjectInfoBytes: projectInfoBytes,
            TotalBytes: total,
            MaxBytes: MaxMemoryBytes);
    }

    /// <inheritdoc/>
    public async Task<MemorySelection> GetMemorySelectionAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var project = await data.GetProjectAsync(projectId, cancellationToken)
            ?? throw new NotFoundException($"No project found with id {projectId}.");
        return project.MemorySelection;
    }

    /// <inheritdoc/>
    public async Task<MemorySelection> UpdateMemorySelectionAsync(Guid projectId, MemorySelection selection, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(selection);
        var project = await data.UpdateMemorySelectionAsync(projectId, selection, cancellationToken)
            ?? throw new NotFoundException($"No project found with id {projectId}.");
        return project.MemorySelection;
    }
}
