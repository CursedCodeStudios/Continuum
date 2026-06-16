namespace Continuum.Models;

/// <summary>
/// Stored playlist state for a single user and list.
/// </summary>
public sealed class ContinuumUserPlaylistState
{
    /// <summary>
    /// Gets or sets the playlist id.
    /// </summary>
    public Guid? PlaylistId { get; set; }

    /// <summary>
    /// Gets or sets the last successful refresh time.
    /// </summary>
    public DateTimeOffset? LastRefreshUtc { get; set; }

    /// <summary>
    /// Gets or sets the last written item count.
    /// </summary>
    public int LastItemCount { get; set; }
}
