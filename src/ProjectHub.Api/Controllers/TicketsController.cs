using ProjectHub.Domain.Models;
using ProjectHub.Services;
using Microsoft.AspNetCore.Mvc;

namespace ProjectHub.Api.Controllers;

[ApiController]
[Route("api/projects/{projectId:guid}/tickets")]
public sealed class TicketsController(ITicketService tickets) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(Guid projectId, CancellationToken cancellationToken)
    {
        var list = await tickets.ListAsync(projectId, cancellationToken);
        return Ok(list.Select(TicketResponse.FromTicket).ToList());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Guid projectId, [FromBody] CreateTicketRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var ticket = await tickets.CreateAsync(projectId, request.Code, request.Title, request.Body, cancellationToken);
        return CreatedAtAction(nameof(List), new { projectId }, TicketResponse.FromTicket(ticket));
    }

    [HttpPost("extract-from-screenshots")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<IActionResult> ExtractFromScreenshots(
        Guid projectId,
        [FromForm(Name = "files")] IFormFileCollection files,
        CancellationToken cancellationToken)
    {
        var screenshots = (files ?? (IFormFileCollection)new FormFileCollection())
            .Select(f => new TicketScreenshot(
                FileName: f.FileName,
                ContentType: f.ContentType,
                Length: f.Length,
                CopyToAsync: (sink, ct) => f.CopyToAsync(sink, ct)))
            .ToList();

        var result = await tickets.ExtractFromScreenshotsAsync(projectId, screenshots, cancellationToken);
        return Ok(result);
    }
}
