using Continuum.Models;

namespace Continuum.Services;

/// <summary>
/// Coordinates full Continuum refresh runs.
/// </summary>
public interface IContinuumRefreshService
{
    /// <summary>
    /// Gets the last refresh result, if any.
    /// </summary>
    ContinuumRefreshResult? LastResult { get; }

    /// <summary>
    /// Gets list summaries for the admin dashboard.
    /// </summary>
    Task<IReadOnlyList<ContinuumAdminListSummary>> GetListSummariesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Refreshes all Continuum playlists.
    /// </summary>
    Task<ContinuumRefreshResult> RefreshAllAsync(
        IProgress<double>? progress,
        CancellationToken cancellationToken);

    /// <summary>
    /// Refreshes a single Continuum list by slug.
    /// </summary>
    Task<ContinuumRefreshResult> RefreshListAsync(
        string slug,
        IProgress<double>? progress,
        CancellationToken cancellationToken);
}
