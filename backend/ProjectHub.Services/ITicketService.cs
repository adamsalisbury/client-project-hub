using ProjectHub.Domain.Models;

namespace ProjectHub.Services;

/// <summary>An uploaded screenshot used as input to ticket extraction.</summary>
public sealed record TicketScreenshot(string FileName, string ContentType, long Length, Func<Stream, CancellationToken, Task> CopyToAsync);

/// <summary>Tickets and screenshot-based extraction.</summary>
public interface ITicketService
{
    Task<IReadOnlyList<Ticket>> ListAsync(Guid projectId, CancellationToken cancellationToken);
    Task<Ticket> CreateAsync(Guid projectId, string code, string title, string body, CancellationToken cancellationToken);
    Task<ExtractedTicket> ExtractFromScreenshotsAsync(Guid projectId, IReadOnlyList<TicketScreenshot> screenshots, CancellationToken cancellationToken);
}
