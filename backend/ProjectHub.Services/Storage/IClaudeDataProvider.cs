using ProjectHub.Domain.Models;

namespace ProjectHub.Services.Storage;

/// <summary>
/// Storage abstraction for Claude Code projects and jobs. Implementations may
/// be backed by JSON, SQL, or any other store; consumers depend only on this
/// interface.
/// </summary>
public interface IClaudeDataProvider
{
    /// <summary>
    /// Creates a new project under a client.
    /// </summary>
    /// <param name="name">Human-readable name for the project.</param>
    /// <param name="workingDirectory">
    /// Absolute filesystem path used as the working directory for every
    /// Claude Code invocation in this project.
    /// </param>
    /// <param name="clientId">The client this project belongs to. Must already exist.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the supplied <paramref name="clientId"/> does not exist.
    /// </exception>
    Task<ClaudeProject> CreateProjectAsync(string name, string workingDirectory, Guid clientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a project by its identifier.
    /// </summary>
    /// <returns>The project, or <see langword="null"/> if not found.</returns>
    Task<ClaudeProject?> GetProjectAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all projects in the order they were created (oldest first).
    /// </summary>
    Task<IReadOnlyList<ClaudeProject>> ListProjectsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists every project belonging to a client, oldest first.
    /// </summary>
    Task<IReadOnlyList<ClaudeProject>> ListProjectsByClientAsync(Guid clientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new job within an existing project, in status <see cref="JobStatus.Queued"/>.
    /// </summary>
    /// <param name="projectId">The project the job belongs to. Must already exist.</param>
    /// <param name="message">The user message to be processed.</param>
    /// <param name="kind">Caller-declared intent (chat or edit). Defaults to <see cref="MessageKind.Chat"/>.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the supplied <paramref name="projectId"/> does not exist.
    /// </exception>
    Task<ClaudeJob> CreateJobAsync(Guid projectId, string message, MessageKind kind = MessageKind.Chat, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a job by its identifier.
    /// </summary>
    /// <returns>The job, or <see langword="null"/> if not found.</returns>
    Task<ClaudeJob?> GetJobAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists changes to an existing job.
    /// </summary>
    Task UpdateJobAsync(ClaudeJob job, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all jobs currently in the supplied status.
    /// </summary>
    Task<IReadOnlyList<ClaudeJob>> ListJobsByStatusAsync(JobStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns every job in a project, ordered by <see cref="ClaudeJob.CreatedAt"/> ascending.
    /// </summary>
    Task<IReadOnlyList<ClaudeJob>> ListJobsByProjectAsync(Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new ticket inside an existing project.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the supplied <paramref name="projectId"/> does not exist.
    /// </exception>
    Task<Ticket> CreateTicketAsync(Guid projectId, string code, string title, string body, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a ticket by its identifier.
    /// </summary>
    Task<Ticket?> GetTicketAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists every ticket in a project, ordered oldest-first.
    /// </summary>
    Task<IReadOnlyList<Ticket>> ListTicketsByProjectAsync(Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new knowledge entry attached to a project.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the supplied <paramref name="projectId"/> does not exist.
    /// </exception>
    Task<KnowledgeEntry> CreateKnowledgeAsync(Guid projectId, string title, string body, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a knowledge entry by its identifier.
    /// </summary>
    Task<KnowledgeEntry?> GetKnowledgeAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists every knowledge entry in a project, ordered oldest-first.
    /// </summary>
    Task<IReadOnlyList<KnowledgeEntry>> ListKnowledgeByProjectAsync(Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a knowledge entry by id. Returns <see langword="true"/> if it was removed.
    /// </summary>
    Task<bool> DeleteKnowledgeAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new client.
    /// </summary>
    Task<ProjectClient> CreateClientAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a client by id.
    /// </summary>
    Task<ProjectClient?> GetClientAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all clients, oldest first.
    /// </summary>
    Task<IReadOnlyList<ProjectClient>> ListClientsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Reassigns a project to a different client.
    /// </summary>
    /// <returns>The updated project, or <see langword="null"/> if it doesn't exist.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no matching client exists.
    /// </exception>
    Task<ClaudeProject?> AssignProjectClientAsync(Guid projectId, Guid clientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new knowledge entry attached to a client.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no matching client exists.
    /// </exception>
    Task<KnowledgeEntry> CreateClientKnowledgeAsync(Guid clientId, string title, string body, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a client knowledge entry by id.
    /// </summary>
    Task<KnowledgeEntry?> GetClientKnowledgeAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists every knowledge entry attached to a client, oldest-first.
    /// </summary>
    Task<IReadOnlyList<KnowledgeEntry>> ListClientKnowledgeAsync(Guid clientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a client knowledge entry by id.
    /// </summary>
    Task<bool> DeleteClientKnowledgeAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new agent attached to a project.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the project does not exist.
    /// </exception>
    Task<Agent> CreateAgentAsync(Guid projectId, string title, string characteristics, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an agent by id.
    /// </summary>
    Task<Agent?> GetAgentAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists every agent attached to a project, oldest-first.
    /// </summary>
    Task<IReadOnlyList<Agent>> ListAgentsByProjectAsync(Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing agent's title and/or characteristics.
    /// </summary>
    /// <returns>The updated agent, or <see langword="null"/> if it doesn't exist.</returns>
    Task<Agent?> UpdateAgentAsync(Guid id, string title, string characteristics, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an agent.
    /// </summary>
    Task<bool> DeleteAgentAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces the project's memory selection.
    /// </summary>
    /// <returns>The updated project, or <see langword="null"/> if it doesn't exist.</returns>
    Task<ClaudeProject?> UpdateMemorySelectionAsync(Guid projectId, MemorySelection selection, CancellationToken cancellationToken = default);
}
