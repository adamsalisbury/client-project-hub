using ProjectHub.Domain.Models;
using ProjectHub.Services;
using Microsoft.AspNetCore.Mvc;

namespace ProjectHub.Api.Controllers;

[ApiController]
[Route("api/projects/{id:guid}")]
public sealed class RepoAnalysisController(IRepoAnalysisService analysis) : ControllerBase
{
    [HttpPost("analyse-repo")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AnalyseRepo(Guid id, [FromBody] AnalyseRepoRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entry = await analysis.AnalyseAsync(id, request.Target, cancellationToken);
        return Ok(new RepoAnalysisResponse(KnowledgeResponse.FromEntry(entry), request.Target));
    }
}
