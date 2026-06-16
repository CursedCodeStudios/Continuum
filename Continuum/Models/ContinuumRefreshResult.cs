namespace Continuum.Models;

/// <summary>
/// Aggregate result of a refresh run.
/// </summary>
public sealed class ContinuumRefreshResult
{
    /// <summary>
    /// Gets or sets when the refresh started.
    /// </summary>
    public DateTimeOffset StartedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets when the refresh completed.
    /// </summary>
    public DateTimeOffset CompletedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the list count processed.
    /// </summary>
    public int ListsProcessed { get; set; }

    /// <summary>
    /// Gets or sets the user count processed.
    /// </summary>
    public int UsersProcessed { get; set; }

    /// <summary>
    /// Gets or sets the number of playlists created.
    /// </summary>
    public int PlaylistsCreated { get; set; }

    /// <summary>
    /// Gets or sets the number of playlists updated.
    /// </summary>
    public int PlaylistsUpdated { get; set; }

    /// <summary>
    /// Gets or sets the number of items resolved.
    /// </summary>
    public int ItemsResolved { get; set; }

    /// <summary>
    /// Gets or sets the number of items missing.
    /// </summary>
    public int ItemsMissing { get; set; }

    /// <summary>
    /// Gets or sets the number of ambiguous items.
    /// </summary>
    public int ItemsAmbiguous { get; set; }

    /// <summary>
    /// Gets or sets warnings encountered during refresh.
    /// </summary>
    public List<string> Warnings { get; set; } = [];
}
