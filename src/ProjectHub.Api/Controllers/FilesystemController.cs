using ProjectHub.Services;
using Microsoft.AspNetCore.Mvc;

namespace ProjectHub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class FilesystemController(IFilesystemService filesystem) : ControllerBase
{
    [HttpGet("browse")]
    public IActionResult Browse([FromQuery] string? path)
        => Ok(filesystem.Browse(path));
}
