using ProjectHub.Services;
using Microsoft.AspNetCore.Mvc;

namespace ProjectHub.Api.Controllers;

[ApiController]
[Route("api/projects/{id:guid}/file")]
public sealed class ProjectGitController(IProjectGitService git) : ControllerBase
{
    [HttpGet("diff")]
    public async Task<IActionResult> Diff(Guid id, [FromQuery] string path, CancellationToken cancellationToken)
        => Ok(await git.GetDiffAsync(id, path, cancellationToken));

    [HttpGet("history")]
    public async Task<IActionResult> History(Guid id, [FromQuery] string path, CancellationToken cancellationToken)
        => Ok(await git.GetHistoryAsync(id, path, cancellationToken));
}
