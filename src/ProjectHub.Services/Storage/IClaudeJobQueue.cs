namespace ProjectHub.Services.Storage;

/// <summary>
/// In-process FIFO queue of job identifiers waiting to be picked up by a worker.
/// </summary>
public interface IClaudeJobQueue
{
    /// <summary>
    /// Enqueues a job for processing.
    /// </summary>
    /// <param name="id">The job identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    ValueTask EnqueueAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads queued job identifiers as they arrive. The sequence completes when
    /// the supplied token is cancelled.
    /// </summary>
    /// <param name="cancellationToken">Token observed by the consumer.</param>
    IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken cancellationToken = default);
}
