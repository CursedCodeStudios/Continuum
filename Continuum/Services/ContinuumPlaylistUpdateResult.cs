namespace Continuum.Services;

/// <summary>
/// Result of a single playlist update attempt.
/// </summary>
public sealed class ContinuumPlaylistUpdateResult
{
    /// <summary>
    /// Gets or sets a value indicating whether a playlist was created.
    /// </summary>
    public bool Created { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether a playlist was updated.
    /// </summary>
    public bool Updated { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the update was skipped for safety.
    /// </summary>
    public bool SkippedForSafety { get; init; }

    /// <summary>
    /// Gets or sets the playlist id.
    /// </summary>
    public Guid? PlaylistId { get; init; }

    /// <summary>
    /// Gets or sets the final item count.
    /// </summary>
    public int ItemCount { get; init; }

    /// <summary>
    /// Gets or sets an optional warning.
    /// </summary>
    public string? Warning { get; init; }
}
