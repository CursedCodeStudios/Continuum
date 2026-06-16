namespace Continuum.Models;

/// <summary>
/// Persisted state for a single Continuum list.
/// </summary>
public sealed class ContinuumListState
{
    /// <summary>
    /// Gets or sets per-user playlist state keyed by user id string.
    /// </summary>
    public Dictionary<string, ContinuumUserPlaylistState> Users { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
