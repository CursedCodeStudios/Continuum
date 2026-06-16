namespace Continuum.Models;

/// <summary>
/// A single resolver strategy attempt emitted for diagnostics.
/// </summary>
public sealed class ContinuumEpisodeResolverTraceStep
{
    /// <summary>
    /// Gets or sets the stable strategy key.
    /// </summary>
    public string StrategyKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name for the strategy.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the attempt status.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the detail message for the step.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets optional candidate summaries for ambiguous steps.
    /// </summary>
    public IReadOnlyList<ContinuumEpisodeResolverItemSummary> Candidates { get; set; } = [];
}
