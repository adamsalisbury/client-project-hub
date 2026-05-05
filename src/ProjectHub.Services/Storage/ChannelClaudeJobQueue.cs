using System.Threading.Channels;

namespace ProjectHub.Services.Storage;

/// <inheritdoc/>
/// <remarks>
/// Backed by an unbounded <see cref="Channel{T}"/>. The queue lives only for
/// the lifetime of the process; the worker re-enqueues persisted
/// <see cref="Models.JobStatus.Queued"/> jobs from the data provider on startup.
/// </remarks>
public sealed class ChannelClaudeJobQueue : IClaudeJobQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false
    });

    /// <inheritdoc/>
    public ValueTask EnqueueAsync(Guid id, CancellationToken cancellationToken = default)
        => _channel.Writer.WriteAsync(id, cancellationToken);

    /// <inheritdoc/>
    public IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken cancellationToken = default)
        => _channel.Reader.ReadAllAsync(cancellationToken);
}
