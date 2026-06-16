namespace Continuum.Models;

/// <summary>
/// Diagnostic episode resolver result shown in the admin UI.
/// </summary>
public sealed class ContinuumEpisodeResolverTestResponse
{
    /// <summary>
    /// Gets or sets the overall outcome.
    /// </summary>
    public string Outcome { get; set; } = "missing";

    /// <summary>
    /// Gets or sets the final matching strategy, if any.
    /// </summary>
    public string? FinalStrategy { get; set; }

    /// <summary>
    /// Gets or sets the top-level status message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the matched item summary when resolved.
    /// </summary>
    public ContinuumEpisodeResolverItemSummary? MatchedItem { get; set; }

    /// <summary>
    /// Gets or sets the detailed strategy trace.
    /// </summary>
    public IReadOnlyList<ContinuumEpisodeResolverTraceStep> Trace { get; set; } = [];
}
