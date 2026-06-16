using Continuum.Models;
using Continuum.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Continuum.Controllers;

/// <summary>
/// Admin endpoints used by the Continuum plugin configuration page.
/// </summary>
[ApiController]
[Authorize(Policy = "RequiresElevation")]
[Route("Continuum/Admin")]
public sealed class ContinuumAdminController(
    IContinuumRefreshService refreshService,
    ILogger<ContinuumAdminController> logger) : ControllerBase
{
    /// <summary>
    /// Gets the current list summaries and last refresh status.
    /// </summary>
    [HttpGet("Lists")]
    public async Task<ActionResult<ContinuumAdminDashboard>> GetLists(CancellationToken cancellationToken)
    {
        IReadOnlyList<ContinuumAdminListSummary> lists = await refreshService
            .GetListSummariesAsync(cancellationToken)
            .ConfigureAwait(false);

        return Ok(new ContinuumAdminDashboard
        {
            Lists = lists,
            LastResult = refreshService.LastResult
        });
    }

    /// <summary>
    /// Refreshes every enabled Continuum list.
    /// </summary>
    [HttpPost("RefreshAll")]
    public async Task<ActionResult<ContinuumRefreshResult>> RefreshAll(CancellationToken cancellationToken)
    {
        logger.LogInformation("Manual Continuum refresh requested for all lists.");

        ContinuumRefreshResult result = await refreshService
            .RefreshAllAsync(progress: null, cancellationToken)
            .ConfigureAwait(false);

        return Ok(result);
    }

    /// <summary>
    /// Refreshes a single Continuum list by slug.
    /// </summary>
    [HttpPost("Lists/{slug}/Refresh")]
    public async Task<ActionResult<ContinuumRefreshResult>> RefreshList(
        [FromRoute] string slug,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Manual Continuum refresh requested for list {Slug}.", slug);

            ContinuumRefreshResult result = await refreshService
                .RefreshListAsync(slug, progress: null, cancellationToken)
                .ConfigureAwait(false);

            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Continuum list not found",
                Detail = $"No enabled Continuum list with slug '{slug}' was found."
            });
        }
    }
}
