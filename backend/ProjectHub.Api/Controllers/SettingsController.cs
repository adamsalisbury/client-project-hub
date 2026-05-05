using ProjectHub.Domain.Models;
using ProjectHub.Services;
using Microsoft.AspNetCore.Mvc;

namespace ProjectHub.Api.Controllers;

/// <summary>Web facade for application-wide settings.</summary>
[ApiController]
[Route("api/[controller]")]
public sealed class SettingsController(ISettingsService settings) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
        => Ok(AppSettingsResponse.FromSettings(await settings.GetAsync(cancellationToken)));

    [HttpPut]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update([FromBody] UpdateAppSettingsRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var saved = await settings.UpdateAsync(new AppSettings { AiName = request.AiName }, cancellationToken);
        return Ok(AppSettingsResponse.FromSettings(saved));
    }
}
