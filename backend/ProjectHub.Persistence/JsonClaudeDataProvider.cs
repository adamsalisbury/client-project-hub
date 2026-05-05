using System.Text.Json;
using System.Text.Json.Serialization;
using ProjectHub.Domain.Models;
using ProjectHub.Services.Storage;
using Microsoft.Extensions.Options;

namespace ProjectHub.Persistence;

/// <inheritdoc/>
/// <remarks>
/// Stores projects and jobs in a single JSON file. The full snapshot is
/// rewritten atomically on every mutation (write to a temp file, then
/// <c>File.Move</c>). All operations are serialised through a
/// <see cref="SemaphoreSlim"/>; a SQL-backed provider should be substituted
/// for any meaningful concurrency.
/// </remarks>
public sealed class JsonClaudeDataProvider : IClaudeDataProvider
{
    private const string DefaultClientName = "Default";

    private static readonly JsonSerializerOptions s_jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _filePath;
    private readonly ILogger<JsonClaudeDataProvider> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly Dictionary<Guid, ClaudeProject> _projects = new();
    private readonly Dictionary<Guid, ClaudeJob> _jobs = new();
    private readonly Dictionary<Guid, Ticket> _tickets = new();
    private readonly Dictionary<Guid, KnowledgeEntry> _knowledge = new();
    private readonly Dictionary<Guid, ProjectClient> _clients = new();
    private readonly Dictionary<Guid, KnowledgeEntry> _clientKnowledge = new();
    private readonly Dictionary<Guid, Agent> _agents = new();
    private readonly Dictionary<Guid, ClientRepo> _clientRepos = new();
    private readonly Dictionary<Guid, Plan> _plans = new();
    private readonly Dictionary<Guid, PlanStep> _planSteps = new();
    private readonly Dictionary<Guid, StepReview> _stepReviews = new();
    private AppSettings _settings = new();
    private bool _initialized;

    public JsonClaudeDataProvider(
        IOptions<JsonDataProviderOptions> options,
        IHostEnvironment environment,
        ILogger<JsonClaudeDataProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(logger);

        var configuredPath = options.Value.FilePath;
        _filePath = Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(environment.ContentRootPath, configuredPath);

        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<ClaudeProject> CreateProjectAsync(string name, Guid clientId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedAsync(cancellationToken);

            if (!_clients.ContainsKey(clientId))
            {
                throw new InvalidOperationException($"Client {clientId} does not exist.");
            }

            var project = new ClaudeProject
            {
                Id = Guid.NewGuid(),
                Name = name.Trim(),
                WorkingDirectory = null,
                ClientId = clientId,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _projects[project.Id] = project;
            await PersistAsync(cancellationToken);

            return Clone(project);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<ClaudeProject?> GetProjectAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedAsync(cancellationToken);
            return _projects.TryGetValue(id, out var project) ? Clone(project) : null;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ClaudeProject>> ListProjectsAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedAsync(cancellationToken);
            return _projects.Values
                .OrderBy(p => p.CreatedAt)
                .Select(Clone)
                .ToList();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ClaudeProject>> ListProjectsByClientAsync(Guid clientId, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedAsync(cancellationToken);
            return _projects.Values
                .Where(p => p.ClientId == clientId)
                .OrderBy(p => p.CreatedAt)
                .Select(Clone)
                .ToList();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<ClaudeJob> CreateJobAsync(
        Guid projectId,
        string message,
        MessageKind kind = MessageKind.Chat,
        JobIntent intent = JobIntent.Conversation,
        Guid? planId = null,
        Guid? planStepId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedAsync(cancellationToken);

            if (!_projects.ContainsKey(projectId))
            {
                throw new InvalidOperationException($"Project {projectId} does not exist.");
            }

            var job = new ClaudeJob
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Message = message,
                Kind = kind,
                Intent = intent,
                PlanId = planId,
                PlanStepId = planStepId,
                Status = JobStatus.Queued,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _jobs[job.Id] = job;
            await PersistAsync(cancellationToken);

            return Clone(job);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<ClaudeJob?> GetJobAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedAsync(cancellationToken);
            return _jobs.TryGetValue(id, out var job) ? Clone(job) : null;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task UpdateJobAsync(ClaudeJob job, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedAsync(cancellationToken);

            if (!_jobs.ContainsKey(job.Id))
            {
                throw new InvalidOperationException($"Job {job.Id} does not exist.");
            }

            _jobs[job.Id] = Clone(job);
            await PersistAsync(cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ClaudeJob>> ListJobsByStatusAsync(JobStatus status, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedAsync(cancellationToken);
            return _jobs.Values
                .Where(j => j.Status == status)
                .Select(Clone)
                .ToList();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ClaudeJob>> ListJobsByProjectAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedAsync(cancellationToken);
            return _jobs.Values
                .Where(j => j.ProjectId == projectId)
                .OrderBy(j => j.CreatedAt)
                .Select(Clone)
                .ToList();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<Ticket> CreateTicketAsync(Guid projectId, string code, string title, string body, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(body);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedAsync(cancellationToken);

            if (!_projects.ContainsKey(projectId))
            {
                throw new InvalidOperationException($"Project {projectId} does not exist.");
            }

            var ticket = new Ticket
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Code = code.Trim(),
                Title = title.Trim(),
                Body = body,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _tickets[ticket.Id] = ticket;
            await PersistAsync(cancellationToken);

            return Clone(ticket);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<Ticket?> GetTicketAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedAsync(cancellationToken);
            return _tickets.TryGetValue(id, out var ticket) ? Clone(ticket) : null;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Ticket>> ListTicketsByProjectAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedAsync(cancellationToken);
            return _tickets.Values
                .Where(t => t.ProjectId == projectId)
                .OrderBy(t => t.CreatedAt)
                .Select(Clone)
                .ToList();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<KnowledgeEntry> CreateKnowledgeAsync(Guid projectId, string title, string body, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(body);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedAsync(cancellationToken);

            if (!_projects.ContainsKey(projectId))
            {
                throw new InvalidOperationException($"Project {projectId} does not exist.");
            }

            var entry = new KnowledgeEntry
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Title = title.Trim(),
                Body = body,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _knowledge[entry.Id] = entry;
            await PersistAsync(cancellationToken);

            return Clone(entry);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<KnowledgeEntry?> GetKnowledgeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedAsync(cancellationToken);
            return _knowledge.TryGetValue(id, out var entry) ? Clone(entry) : null;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<KnowledgeEntry>> ListKnowledgeByProjectAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedAsync(cancellationToken);
            return _knowledge.Values
                .Where(k => k.ProjectId == projectId)
                .OrderBy(k => k.CreatedAt)
                .Select(Clone)
                .ToList();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteKnowledgeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedAsync(cancellationToken);
            if (!_knowledge.Remove(id))
            {
                return false;
            }
            await PersistAsync(cancellationToken);
            return true;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<ProjectClient> CreateClientAsync(string name, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedAsync(cancellationToken);

            var client = new ProjectClient
            {
                Id = Guid.NewGuid(),
                Name = name.Trim(),
                Colour = ClientColours.AutoAssign(_clients.Count),
                CreatedAt = DateTimeOffset.UtcNow
            };

            _clients[client.Id] = client;
            await PersistAsync(cancellationToken);
            return Clone(client);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<ProjectClient?> GetClientAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedAsync(cancellationToken);
            return _clients.TryGetValue(id, out var c) ? Clone(c) : null;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ProjectClient>> ListClientsAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedAsync(cancellationToken);
            return _clients.Values
                .OrderBy(c => c.CreatedAt)
                .Select(Clone)
                .ToList();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<ClaudeProject?> AssignProjectClientAsync(Guid projectId, Guid clientId, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedAsync(cancellationToken);
            if (!_projects.TryGetValue(projectId, out var project))
            {
                return null;
            }
            if (!_clients.ContainsKey(clientId))
            {
                throw new InvalidOperationException($"Client {clientId} does not exist.");
            }
            project.ClientId = clientId;
            await PersistAsync(cancellationToken);
            return Clone(project);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<KnowledgeEntry> CreateClientKnowledgeAsync(Guid clientId, string title, string body, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(body);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedAsync(cancellationToken);
            if (!_clients.ContainsKey(clientId))
            {
                throw new InvalidOperationException($"Client {clientId} does not exist.");
            }

            var entry = new KnowledgeEntry
            {
                Id = Guid.NewGuid(),
                ProjectId = null,
                ClientId = clientId,
                Title = title.Trim(),
                Body = body,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _clientKnowledge[entry.Id] = entry;
            await PersistAsync(cancellationToken);
            return Clone(entry);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<KnowledgeEntry?> GetClientKnowledgeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedAsync(cancellationToken);
            return _clientKnowledge.TryGetValue(id, out var e) ? Clone(e) : null;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<KnowledgeEntry>> ListClientKnowledgeAsync(Guid clientId, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedAsync(cancellationToken);
            return _clientKnowledge.Values
                .Where(k => k.ClientId == clientId)
                .OrderBy(k => k.CreatedAt)
                .Select(Clone)
                .ToList();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteClientKnowledgeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedAsync(cancellationToken);
            if (!_clientKnowledge.Remove(id))
            {
                return false;
            }
            await PersistAsync(cancellationToken);
            return true;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<Agent> CreateAgentAsync(Guid projectId, string title, string characteristics, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(characteristics);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedAsync(cancellationToken);
            if (!_projects.ContainsKey(projectId))
            {
                throw new InvalidOperationException($"Project {projectId} does not exist.");
            }

            var agent = new Agent
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Title = title.Trim(),
                Characteristics = characteristics,
                CreatedAt = DateTimeOffset.UtcNow
            };
            _agents[agent.Id] = agent;
            await PersistAsync(cancellationToken);
            return Clone(agent);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<Agent?> GetAgentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedAsync(cancellationToken);
            return _agents.TryGetValue(id, out var a) ? Clone(a) : null;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Agent>> ListAgentsByProjectAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedAsync(cancellationToken);
            return _agents.Values
                .Where(a => a.ProjectId == projectId)
                .OrderBy(a => a.CreatedAt)
                .Select(Clone)
                .ToList();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<Agent?> UpdateAgentAsync(Guid id, string title, string characteristics, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(characteristics);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedAsync(cancellationToken);
            if (!_agents.TryGetValue(id, out var existing))
            {
                return null;
            }
            existing.Title = title.Trim();
            existing.Characteristics = characteristics;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
            await PersistAsync(cancellationToken);
            return Clone(existing);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAgentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedAsync(cancellationToken);
            if (!_agents.Remove(id))
            {
                return false;
            }
            await PersistAsync(cancellationToken);
            return true;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<ClaudeProject?> UpdateMemorySelectionAsync(Guid projectId, MemorySelection selection, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(selection);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedAsync(cancellationToken);
            if (!_projects.TryGetValue(projectId, out var project))
            {
                return null;
            }
            project.MemorySelection = CloneSelection(selection);
            await PersistAsync(cancellationToken);
            return Clone(project);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<ClaudeProject?> UpdateProjectAsync(
        Guid projectId,
        string? description = null,
        Guid? ticketId = null,
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedAsync(cancellationToken);
            if (!_projects.TryGetValue(projectId, out var project))
            {
                return null;
            }

            if (description is not null)
            {
                project.Description = description;
            }
            if (ticketId is not null)
            {
                project.TicketId = ticketId == Guid.Empty ? null : ticketId;
            }

            await PersistAsync(cancellationToken);
            return Clone(project);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<ProjectClient?> UpdateClientColourAsync(Guid clientId, string colour, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(colour);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedAsync(cancellationToken);
            if (!_clients.TryGetValue(clientId, out var client))
            {
                return null;
            }
            client.Colour = colour;
            await PersistAsync(cancellationToken);
            return Clone(client);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<ClientRepo> CreateClientRepoAsync(Guid clientId, string name, string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedAsync(cancellationToken);
            if (!_clients.ContainsKey(clientId))
            {
                throw new InvalidOperationException($"Client {clientId} does not exist.");
            }

            var repo = new ClientRepo
            {
                Id = Guid.NewGuid(),
                ClientId = clientId,
                Name = name.Trim(),
                Path = path,
                CreatedAt = DateTimeOffset.UtcNow
            };
            _clientRepos[repo.Id] = repo;
            await PersistAsync(cancellationToken);
            return Clone(repo);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<ClientRepo?> GetClientRepoAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedAsync(cancellationToken);
            return _clientRepos.TryGetValue(id, out var r) ? Clone(r) : null;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ClientRepo>> ListClientReposAsync(Guid clientId, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedAsync(cancellationToken);
            return _clientRepos.Values
                .Where(r => r.ClientId == clientId)
                .OrderBy(r => r.CreatedAt)
                .Select(Clone)
                .ToList();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteClientRepoAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedAsync(cancellationToken);
            if (!_clientRepos.Remove(id))
            {
                return false;
            }
            // Detach any project that pointed at this repo.
            foreach (var project in _projects.Values.Where(p => p.RepoId == id))
            {
                project.RepoId = null;
            }
            await PersistAsync(cancellationToken);
            return true;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<ClaudeProject?> AssignProjectRepoAsync(Guid projectId, Guid? repoId, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedAsync(cancellationToken);
            if (!_projects.TryGetValue(projectId, out var project))
            {
                return null;
            }

            if (repoId is null)
            {
                project.RepoId = null;
            }
            else
            {
                if (!_clientRepos.TryGetValue(repoId.Value, out var repo))
                {
                    throw new InvalidOperationException($"Repo {repoId} does not exist.");
                }
                if (repo.ClientId != project.ClientId)
                {
                    throw new InvalidOperationException(
                        $"Repo {repoId} belongs to client {repo.ClientId}, not the project's client {project.ClientId}.");
                }
                project.RepoId = repo.Id;
                project.WorkingDirectory = repo.Path;
            }

            await PersistAsync(cancellationToken);
            return Clone(project);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<Plan> GetOrCreatePlanAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedAsync(cancellationToken);
            if (!_projects.ContainsKey(projectId))
            {
                throw new InvalidOperationException($"Project {projectId} does not exist.");
            }

            var existing = _plans.Values.FirstOrDefault(p => p.ProjectId == projectId);
            if (existing is not null)
            {
                return Clone(existing);
            }

            var plan = new Plan
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                CreatedAt = DateTimeOffset.UtcNow
            };
            _plans[plan.Id] = plan;
            await PersistAsync(cancellationToken);
            return Clone(plan);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<PlanStep>> ListPlanStepsAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedAsync(cancellationToken);
            return _planSteps.Values
                .Where(s => s.PlanId == planId)
                .OrderBy(s => s.Order)
                .Select(Clone)
                .ToList();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<PlanStep> AddPlanStepAsync(Guid planId, string title, string description, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(description);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedAsync(cancellationToken);
            if (!_plans.TryGetValue(planId, out var plan))
            {
                throw new InvalidOperationException($"Plan {planId} does not exist.");
            }

            var nextOrder = _planSteps.Values
                .Where(s => s.PlanId == planId)
                .Select(s => (int?)s.Order)
                .Max() + 1 ?? 0;

            var step = new PlanStep
            {
                Id = Guid.NewGuid(),
                PlanId = planId,
                Order = nextOrder,
                Title = title.Trim(),
                Description = description
            };
            _planSteps[step.Id] = step;
            plan.UpdatedAt = DateTimeOffset.UtcNow;

            await PersistAsync(cancellationToken);
            return Clone(step);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<PlanStep?> UpdatePlanStepAsync(Guid stepId, string title, string description, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(description);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedAsync(cancellationToken);
            if (!_planSteps.TryGetValue(stepId, out var step))
            {
                return null;
            }
            step.Title = title.Trim();
            step.Description = description;
            if (_plans.TryGetValue(step.PlanId, out var plan))
            {
                plan.UpdatedAt = DateTimeOffset.UtcNow;
            }
            await PersistAsync(cancellationToken);
            return Clone(step);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<PlanStep?> UpdatePlanStepStatusAsync(
        Guid stepId,
        PlanStepStatus status,
        Guid? jobId = null,
        DateTimeOffset? startedAt = null,
        DateTimeOffset? completedAt = null,
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedAsync(cancellationToken);
            if (!_planSteps.TryGetValue(stepId, out var step))
            {
                return null;
            }
            step.Status = status;
            if (jobId is not null) step.JobId = jobId;
            if (startedAt is not null) step.StartedAt = startedAt;
            if (completedAt is not null) step.CompletedAt = completedAt;
            await PersistAsync(cancellationToken);
            return Clone(step);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<bool> DeletePlanStepAsync(Guid stepId, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedAsync(cancellationToken);
            if (!_planSteps.TryGetValue(stepId, out var step))
            {
                return false;
            }
            var planId = step.PlanId;
            _planSteps.Remove(stepId);

            // Compact the order of remaining steps in the same plan.
            var remaining = _planSteps.Values.Where(s => s.PlanId == planId).OrderBy(s => s.Order).ToList();
            for (var i = 0; i < remaining.Count; i++)
            {
                remaining[i].Order = i;
            }

            if (_plans.TryGetValue(planId, out var plan))
            {
                plan.UpdatedAt = DateTimeOffset.UtcNow;
            }
            await PersistAsync(cancellationToken);
            return true;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<PlanStep>> ReorderPlanStepsAsync(Guid planId, IReadOnlyList<Guid> orderedStepIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(orderedStepIds);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedAsync(cancellationToken);
            if (!_plans.TryGetValue(planId, out var plan))
            {
                throw new InvalidOperationException($"Plan {planId} does not exist.");
            }

            var current = _planSteps.Values.Where(s => s.PlanId == planId).ToDictionary(s => s.Id);
            if (current.Count != orderedStepIds.Count || orderedStepIds.Any(id => !current.ContainsKey(id)) || orderedStepIds.Distinct().Count() != orderedStepIds.Count)
            {
                throw new InvalidOperationException("Reorder list must contain every step in the plan exactly once.");
            }

            for (var i = 0; i < orderedStepIds.Count; i++)
            {
                current[orderedStepIds[i]].Order = i;
            }
            plan.UpdatedAt = DateTimeOffset.UtcNow;
            await PersistAsync(cancellationToken);

            return current.Values.OrderBy(s => s.Order).Select(Clone).ToList();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<Plan?> RecordPlanVerificationAsync(Guid planId, string opinion, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(opinion);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedAsync(cancellationToken);
            if (!_plans.TryGetValue(planId, out var plan))
            {
                return null;
            }
            plan.LastVerificationOpinion = opinion;
            plan.LastVerifiedAt = DateTimeOffset.UtcNow;
            await PersistAsync(cancellationToken);
            return Clone(plan);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<StepReview> CreateStepReviewAsync(Guid projectId, Guid stepId, Guid jobId, IReadOnlyList<string> files, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(files);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedAsync(cancellationToken);
            var review = new StepReview
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                StepId = stepId,
                JobId = jobId,
                CreatedAt = DateTimeOffset.UtcNow,
                Files = files.Select(f => new StepReviewFile { Path = f, State = StepReviewFileState.Pending }).ToList()
            };
            _stepReviews[review.Id] = review;
            await PersistAsync(cancellationToken);
            return Clone(review);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<StepReview?> GetStepReviewAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedAsync(cancellationToken);
            return _stepReviews.TryGetValue(id, out var r) ? Clone(r) : null;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<StepReview?> UpdateStepReviewFileAsync(Guid reviewId, string path, StepReviewFileState state, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedAsync(cancellationToken);
            if (!_stepReviews.TryGetValue(reviewId, out var review))
            {
                return null;
            }
            var file = review.Files.FirstOrDefault(f => string.Equals(f.Path, path, StringComparison.Ordinal));
            if (file is null)
            {
                return null;
            }
            file.State = state;
            file.ResolvedAt = state == StepReviewFileState.Pending ? null : DateTimeOffset.UtcNow;
            await PersistAsync(cancellationToken);
            return Clone(review);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<StepReview>> ListStepReviewsByProjectAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedAsync(cancellationToken);
            return _stepReviews.Values
                .Where(r => r.ProjectId == projectId)
                .OrderByDescending(r => r.CreatedAt)
                .Select(Clone)
                .ToList();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<AppSettings> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedAsync(cancellationToken);
            return Clone(_settings);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<AppSettings> UpdateSettingsAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedAsync(cancellationToken);
            _settings = Clone(settings);
            await PersistAsync(cancellationToken);
            return Clone(_settings);
        }
        finally
        {
            _lock.Release();
        }
    }

    private static AppSettings Clone(AppSettings settings) => new()
    {
        AiName = settings.AiName
    };

    private async Task EnsureLoadedAsync(CancellationToken cancellationToken)
    {
        if (_initialized)
        {
            return;
        }

        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var dirty = false;

        if (File.Exists(_filePath))
        {
            try
            {
                await using var stream = File.OpenRead(_filePath);
                var snapshot = await JsonSerializer.DeserializeAsync<ClaudeDataSnapshot>(
                    stream,
                    s_jsonOptions,
                    cancellationToken);

                if (snapshot is not null)
                {
                    foreach (var (id, project) in snapshot.Projects)
                    {
                        _projects[id] = project;
                    }

                    foreach (var (id, job) in snapshot.Jobs)
                    {
                        _jobs[id] = job;
                    }

                    foreach (var (id, ticket) in snapshot.Tickets)
                    {
                        _tickets[id] = ticket;
                    }

                    foreach (var (id, entry) in snapshot.Knowledge)
                    {
                        _knowledge[id] = entry;
                    }

                    foreach (var (id, client) in snapshot.Clients)
                    {
                        _clients[id] = client;
                    }

                    foreach (var (id, entry) in snapshot.ClientKnowledge)
                    {
                        _clientKnowledge[id] = entry;
                    }

                    foreach (var (id, agent) in snapshot.Agents)
                    {
                        _agents[id] = agent;
                    }

                    foreach (var (id, repo) in snapshot.ClientRepos)
                    {
                        _clientRepos[id] = repo;
                    }

                    foreach (var (id, plan) in snapshot.Plans)
                    {
                        _plans[id] = plan;
                    }

                    foreach (var (id, step) in snapshot.PlanSteps)
                    {
                        _planSteps[id] = step;
                    }

                    foreach (var (id, review) in snapshot.StepReviews)
                    {
                        _stepReviews[id] = review;
                    }

                    _settings = snapshot.Settings ?? new AppSettings();
                }

                // Backfill default memory selection for any projects loaded
                // from older data files where it was absent.
                foreach (var project in _projects.Values)
                {
                    project.MemorySelection ??= new MemorySelection();
                }

                if (_projects.Values.Any(p => p.ClientId == Guid.Empty))
                {
                    var defaultClient = _clients.Values.FirstOrDefault(c => c.Name == DefaultClientName)
                        ?? CreateDefaultClient();
                    foreach (var project in _projects.Values.Where(p => p.ClientId == Guid.Empty))
                    {
                        project.ClientId = defaultClient.Id;
                    }
                    dirty = true;
                }

                // Backfill colours for clients loaded without one.
                foreach (var client in _clients.Values.Where(c => string.IsNullOrEmpty(c.Colour)))
                {
                    client.Colour = ClientColours.Default;
                    dirty = true;
                }

                // Backfill: each project with a working directory but no
                // RepoId gets a synthesised ClientRepo on its client and a
                // link to it, so the new repo-pointer model works even with
                // legacy data.
                foreach (var project in _projects.Values.Where(p => p.RepoId is null && !string.IsNullOrWhiteSpace(p.WorkingDirectory)))
                {
                    var workingDirectory = project.WorkingDirectory!;
                    var existing = _clientRepos.Values.FirstOrDefault(r =>
                        r.ClientId == project.ClientId &&
                        string.Equals(r.Path, workingDirectory, StringComparison.Ordinal));

                    if (existing is not null)
                    {
                        project.RepoId = existing.Id;
                    }
                    else
                    {
                        var repo = new ClientRepo
                        {
                            Id = Guid.NewGuid(),
                            ClientId = project.ClientId,
                            Name = DeriveRepoName(workingDirectory),
                            Path = workingDirectory,
                            CreatedAt = DateTimeOffset.UtcNow
                        };
                        _clientRepos[repo.Id] = repo;
                        project.RepoId = repo.Id;
                    }
                    dirty = true;
                }

                _logger.LogInformation(
                    "Loaded {ProjectCount} projects, {JobCount} jobs, {TicketCount} tickets, {KnowledgeCount} knowledge entries, {ClientCount} clients, {ClientKnowledgeCount} client knowledge entries, {AgentCount} agents, {RepoCount} client repos, {PlanCount} plans, {PlanStepCount} plan steps, {StepReviewCount} step reviews from {FilePath}",
                    _projects.Count,
                    _jobs.Count,
                    _tickets.Count,
                    _knowledge.Count,
                    _clients.Count,
                    _clientKnowledge.Count,
                    _agents.Count,
                    _clientRepos.Count,
                    _plans.Count,
                    _planSteps.Count,
                    _stepReviews.Count,
                    _filePath);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse {FilePath}; starting with an empty store.", _filePath);
            }
        }
        else
        {
            _logger.LogInformation("No existing data store at {FilePath}; starting empty.", _filePath);
        }

        _initialized = true;

        if (dirty)
        {
            await PersistAsync(cancellationToken);
        }
    }

    private ProjectClient CreateDefaultClient()
    {
        var client = new ProjectClient
        {
            Id = Guid.NewGuid(),
            Name = DefaultClientName,
            Colour = ClientColours.AutoAssign(_clients.Count),
            CreatedAt = DateTimeOffset.UtcNow
        };
        _clients[client.Id] = client;
        _logger.LogInformation("Created '{ClientName}' client {ClientId} for orphaned projects.", DefaultClientName, client.Id);
        return client;
    }

    private async Task PersistAsync(CancellationToken cancellationToken)
    {
        var snapshot = new ClaudeDataSnapshot
        {
            Projects = new Dictionary<Guid, ClaudeProject>(_projects),
            Jobs = new Dictionary<Guid, ClaudeJob>(_jobs),
            Tickets = new Dictionary<Guid, Ticket>(_tickets),
            Knowledge = new Dictionary<Guid, KnowledgeEntry>(_knowledge),
            Clients = new Dictionary<Guid, ProjectClient>(_clients),
            ClientKnowledge = new Dictionary<Guid, KnowledgeEntry>(_clientKnowledge),
            Agents = new Dictionary<Guid, Agent>(_agents),
            ClientRepos = new Dictionary<Guid, ClientRepo>(_clientRepos),
            Plans = new Dictionary<Guid, Plan>(_plans),
            PlanSteps = new Dictionary<Guid, PlanStep>(_planSteps),
            StepReviews = new Dictionary<Guid, StepReview>(_stepReviews),
            Settings = Clone(_settings)
        };

        var tempPath = _filePath + ".tmp";

        await using (var stream = File.Create(tempPath))
        {
            await JsonSerializer.SerializeAsync(stream, snapshot, s_jsonOptions, cancellationToken);
        }

        File.Move(tempPath, _filePath, overwrite: true);
    }

    private static ClaudeProject Clone(ClaudeProject project) => new()
    {
        Id = project.Id,
        Name = project.Name,
        WorkingDirectory = project.WorkingDirectory,
        CreatedAt = project.CreatedAt,
        ClientId = project.ClientId,
        RepoId = project.RepoId,
        Description = project.Description,
        TicketId = project.TicketId,
        MemorySelection = CloneSelection(project.MemorySelection)
    };

    private static MemorySelection CloneSelection(MemorySelection selection) => new()
    {
        IncludeProjectInfo = selection.IncludeProjectInfo,
        IncludeClientInfo = selection.IncludeClientInfo,
        ExcludedAgentIds = new List<Guid>(selection.ExcludedAgentIds),
        ExcludedTicketIds = new List<Guid>(selection.ExcludedTicketIds),
        ExcludedProjectKnowledgeIds = new List<Guid>(selection.ExcludedProjectKnowledgeIds),
        ExcludedClientKnowledgeIds = new List<Guid>(selection.ExcludedClientKnowledgeIds),
        ExcludedConversationJobIds = new List<Guid>(selection.ExcludedConversationJobIds),
        SectionSummaries = selection.SectionSummaries.ToDictionary(
            kvp => kvp.Key,
            kvp => new MemorySectionSummary
            {
                Body = kvp.Value.Body,
                GeneratedAt = kvp.Value.GeneratedAt,
                Included = kvp.Value.Included
            })
    };

    private static Agent Clone(Agent agent) => new()
    {
        Id = agent.Id,
        ProjectId = agent.ProjectId,
        Title = agent.Title,
        Characteristics = agent.Characteristics,
        CreatedAt = agent.CreatedAt,
        UpdatedAt = agent.UpdatedAt
    };

    private static ProjectClient Clone(ProjectClient client) => new()
    {
        Id = client.Id,
        Name = client.Name,
        Colour = client.Colour,
        CreatedAt = client.CreatedAt
    };

    private static ClientRepo Clone(ClientRepo repo) => new()
    {
        Id = repo.Id,
        ClientId = repo.ClientId,
        Name = repo.Name,
        Path = repo.Path,
        CreatedAt = repo.CreatedAt
    };

    private static Plan Clone(Plan plan) => new()
    {
        Id = plan.Id,
        ProjectId = plan.ProjectId,
        CreatedAt = plan.CreatedAt,
        UpdatedAt = plan.UpdatedAt,
        LastVerificationOpinion = plan.LastVerificationOpinion,
        LastVerifiedAt = plan.LastVerifiedAt
    };

    private static PlanStep Clone(PlanStep step) => new()
    {
        Id = step.Id,
        PlanId = step.PlanId,
        Order = step.Order,
        Title = step.Title,
        Description = step.Description,
        Status = step.Status,
        JobId = step.JobId,
        StartedAt = step.StartedAt,
        CompletedAt = step.CompletedAt
    };

    private static StepReview Clone(StepReview review) => new()
    {
        Id = review.Id,
        ProjectId = review.ProjectId,
        StepId = review.StepId,
        JobId = review.JobId,
        CreatedAt = review.CreatedAt,
        Files = review.Files
            .Select(f => new StepReviewFile { Path = f.Path, State = f.State, ResolvedAt = f.ResolvedAt })
            .ToList()
    };

    private static string DeriveRepoName(string workingDirectory)
    {
        var trimmed = workingDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var leaf = Path.GetFileName(trimmed);
        return string.IsNullOrEmpty(leaf) ? workingDirectory : leaf;
    }

    private static KnowledgeEntry Clone(KnowledgeEntry entry) => new()
    {
        Id = entry.Id,
        ProjectId = entry.ProjectId,
        ClientId = entry.ClientId,
        Title = entry.Title,
        Body = entry.Body,
        CreatedAt = entry.CreatedAt,
        UpdatedAt = entry.UpdatedAt
    };

    private static Ticket Clone(Ticket ticket) => new()
    {
        Id = ticket.Id,
        ProjectId = ticket.ProjectId,
        Code = ticket.Code,
        Title = ticket.Title,
        Body = ticket.Body,
        CreatedAt = ticket.CreatedAt,
        UpdatedAt = ticket.UpdatedAt
    };

    private static ClaudeJob Clone(ClaudeJob job) => new()
    {
        Id = job.Id,
        ProjectId = job.ProjectId,
        Message = job.Message,
        CreatedAt = job.CreatedAt,
        Kind = job.Kind,
        Intent = job.Intent,
        PlanId = job.PlanId,
        PlanStepId = job.PlanStepId,
        Status = job.Status,
        Prompt = job.Prompt,
        Response = job.Response,
        Error = job.Error,
        ExitCode = job.ExitCode,
        DurationMs = job.DurationMs,
        StartedAt = job.StartedAt,
        CompletedAt = job.CompletedAt,
        FilesChanged = job.FilesChanged.ToArray()
    };
}
