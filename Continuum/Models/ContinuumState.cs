namespace Continuum.Models;

/// <summary>
/// Persisted Continuum state.
/// </summary>
public sealed class ContinuumState
{
    /// <summary>
    /// Gets or sets the state keyed by list slug.
    /// </summary>
    public Dictionary<string, ContinuumListState> Lists { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
