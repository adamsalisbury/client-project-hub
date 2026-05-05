using ProjectHub.Services;
using Microsoft.AspNetCore.Mvc;

namespace ProjectHub.Api.Controllers;

[ApiController]
[Route("api/projects/{id:guid}")]
public sealed class ProjectFilesController(IProjectFileService files) : ControllerBase
{
    [HttpGet("files")]
    public async Task<IActionResult> ListFiles(Guid id, [FromQuery] string? path, CancellationToken cancellationToken)
        => Ok(await files.ListAsync(id, path, cancellationToken));

    [HttpGet("file")]
    public async Task<IActionResult> ReadFile(Guid id, [FromQuery] string path, CancellationToken cancellationToken)
        => Ok(await files.ReadAsync(id, path, cancellationToken));
}
