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
    /// Creates a new project under a client. The project starts with no repo
    /// link and no working directory; call <see cref="AssignProjectRepoAsync"/>
    /// to attach one.
    /// </summary>
    /// <param name="name">Human-readable name for the project.</param>
    /// <param name="clientId">The client this project belongs to. Must already exist.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the supplied <paramref name="clientId"/> does not exist.
    /// </exception>
    Task<ClaudeProject> CreateProjectAsync(string name, Guid clientId, CancellationToken cancellationToken = default);

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
    Task<ClaudeJob> CreateJobAsync(
        Guid projectId,
        string message,
        MessageKind kind = MessageKind.Chat,
        JobIntent intent = JobIntent.Conversation,
        Guid? planId = null,
        Guid? planStepId = null,
        CancellationToken cancellationToken = default);

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

    /// <summary>
    /// Updates the editable fields of a project (description, primary ticket
    /// pointer). Only non-null arguments are written.
    /// </summary>
    /// <returns>The updated project, or <see langword="null"/> if not found.</returns>
    Task<ClaudeProject?> UpdateProjectAsync(
        Guid projectId,
        string? description = null,
        Guid? ticketId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a client's tab colour. Caller is responsible for validating the
    /// hex string before calling.
    /// </summary>
    Task<ProjectClient?> UpdateClientColourAsync(Guid clientId, string colour, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a new repo against a client. <paramref name="path"/> is
    /// stored verbatim (the service layer is expected to normalise it).
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the client does not exist.</exception>
    Task<ClientRepo> CreateClientRepoAsync(Guid clientId, string name, string path, CancellationToken cancellationToken = default);

    /// <summary>Retrieves a single repo by id.</summary>
    Task<ClientRepo?> GetClientRepoAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Lists every repo registered against a client, oldest first.</summary>
    Task<IReadOnlyList<ClientRepo>> ListClientReposAsync(Guid clientId, CancellationToken cancellationToken = default);

    /// <summary>Removes a repo. Projects that linked to it are left dangling (RepoId cleared).</summary>
    Task<bool> DeleteClientRepoAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Links a project to one of its client's repos (or clears the link when
    /// <paramref name="repoId"/> is null). When linked, the project's
    /// <see cref="ClaudeProject.WorkingDirectory"/> is updated to match.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the repo exists but does not belong to the project's client.
    /// </exception>
    Task<ClaudeProject?> AssignProjectRepoAsync(Guid projectId, Guid? repoId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the project's plan, creating an empty one on first access.
    /// </summary>
    Task<Plan> GetOrCreatePlanAsync(Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>Returns every step of a plan in order.</summary>
    Task<IReadOnlyList<PlanStep>> ListPlanStepsAsync(Guid planId, CancellationToken cancellationToken = default);

    /// <summary>Appends a new step to the end of a plan.</summary>
    Task<PlanStep> AddPlanStepAsync(Guid planId, string title, string description, CancellationToken cancellationToken = default);

    /// <summary>Updates a step's title and/or description.</summary>
    Task<PlanStep?> UpdatePlanStepAsync(Guid stepId, string title, string description, CancellationToken cancellationToken = default);

    /// <summary>Replaces a step's status (and optional timestamps / job-id metadata).</summary>
    Task<PlanStep?> UpdatePlanStepStatusAsync(
        Guid stepId,
        PlanStepStatus status,
        Guid? jobId = null,
        DateTimeOffset? startedAt = null,
        DateTimeOffset? completedAt = null,
        CancellationToken cancellationToken = default);

    /// <summary>Deletes a step and compacts the order of remaining steps.</summary>
    Task<bool> DeletePlanStepAsync(Guid stepId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces the order of a plan's steps. <paramref name="orderedStepIds"/>
    /// must list every step belonging to the plan exactly once.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the supplied list does not match the plan's steps.
    /// </exception>
    Task<IReadOnlyList<PlanStep>> ReorderPlanStepsAsync(Guid planId, IReadOnlyList<Guid> orderedStepIds, CancellationToken cancellationToken = default);

    /// <summary>Stores the most recent verification opinion against a plan.</summary>
    Task<Plan?> RecordPlanVerificationAsync(Guid planId, string opinion, CancellationToken cancellationToken = default);

    /// <summary>Creates a step review snapshotting the files Claude touched.</summary>
    Task<StepReview> CreateStepReviewAsync(Guid projectId, Guid stepId, Guid jobId, IReadOnlyList<string> files, CancellationToken cancellationToken = default);

    /// <summary>Retrieves a step review by id.</summary>
    Task<StepReview?> GetStepReviewAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Updates the per-file commit/rollback state on an existing review.</summary>
    Task<StepReview?> UpdateStepReviewFileAsync(Guid reviewId, string path, StepReviewFileState state, CancellationToken cancellationToken = default);

    /// <summary>Lists every review owned by a project, newest first.</summary>
    Task<IReadOnlyList<StepReview>> ListStepReviewsByProjectAsync(Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>Returns the persisted application settings (always non-null).</summary>
    Task<AppSettings> GetSettingsAsync(CancellationToken cancellationToken = default);

    /// <summary>Replaces the persisted application settings.</summary>
    Task<AppSettings> UpdateSettingsAsync(AppSettings settings, CancellationToken cancellationToken = default);
}
