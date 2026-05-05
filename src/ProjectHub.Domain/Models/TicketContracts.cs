namespace ProjectHub.Domain.Models;

/// <summary>
/// Request body for <c>POST /api/projects/{id}/tickets</c>.
/// </summary>
public sealed record CreateTicketRequest(string Code, string Title, string Body);

/// <summary>
/// Wire format for a ticket.
/// </summary>
public sealed record TicketResponse(
    Guid Id,
    Guid ProjectId,
    string Code,
    string Title,
    string Body,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt)
{
    public static TicketResponse FromTicket(Ticket ticket) => new(
        ticket.Id,
        ticket.ProjectId,
        ticket.Code,
        ticket.Title,
        ticket.Body,
        ticket.CreatedAt,
        ticket.UpdatedAt);
}

/// <summary>
/// Returned by the screenshot-extraction endpoint with whatever Claude
/// produced. The user reviews these before saving.
/// </summary>
public sealed record ExtractedTicket(string Code, string Title, string Body);
