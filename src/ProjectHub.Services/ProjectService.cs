using System.Text;
using ProjectHub.Domain.Models;
using ProjectHub.Services.Storage;

namespace ProjectHub.Services;

/// <inheritdoc/>
public sealed class ProjectService(IClaudeDataProvider data, ILogger<ProjectService> logger) : IProjectService
{
    private const long MaxMemoryBytes = 1_000_000;

    /// <inheritdoc/>
    public async Task<ClaudeProject> CreateAsync(string name, string workingDirectory, Guid clientId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationException("The 'name' field is required.");
        }
        if (string.IsNullOrWhiteSpace(workingDirectory))
        {
            throw new ValidationException("The 'workingDirectory' field is required.");
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

        var project = await data.CreateProjectAsync(name, resolved, clientId, cancellationToken);
        logger.LogInformation(
            "Created project {ProjectId} named '{Name}' under client {ClientId} (cwd: {WorkingDirectory})",
            project.Id, project.Name, project.ClientId, project.WorkingDirectory);
        return project;
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
