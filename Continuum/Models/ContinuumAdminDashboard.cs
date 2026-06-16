namespace Continuum.Models;

/// <summary>
/// Data returned to the admin dashboard page.
/// </summary>
public sealed class ContinuumAdminDashboard
{
    /// <summary>
    /// Gets or sets the loaded list summaries.
    /// </summary>
    public IReadOnlyList<ContinuumAdminListSummary> Lists { get; set; } = [];

    /// <summary>
    /// Gets or sets the current refresh operation status.
    /// </summary>
    public ContinuumRefreshOperationStatus OperationStatus { get; set; } = ContinuumRefreshOperationStatus.CreateIdle();

    /// <summary>
    /// Gets or sets the last refresh result, if any.
    /// </summary>
    public ContinuumRefreshResult? LastResult { get; set; }
}
