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
    /// Refreshes all Continuum playlists.
    /// </summary>
    Task<ContinuumRefreshResult> RefreshAllAsync(
        IProgress<double>? progress,
        CancellationToken cancellationToken);
}
