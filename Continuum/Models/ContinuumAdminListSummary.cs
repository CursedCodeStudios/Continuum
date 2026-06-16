namespace Continuum.Models;

/// <summary>
/// Summary data for a single Continuum list in the admin dashboard.
/// </summary>
public sealed class ContinuumAdminListSummary
{
    /// <summary>
    /// Gets or sets the list display name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the stable list slug.
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the list is enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the number of archive items in the list definition.
    /// </summary>
    public int ItemCount { get; set; }

    /// <summary>
    /// Gets or sets the number of persisted user playlists tracked for the list.
    /// </summary>
    public int PlaylistCount { get; set; }

    /// <summary>
    /// Gets or sets the last refresh time across tracked users.
    /// </summary>
    public DateTimeOffset? LastRefreshUtc { get; set; }

    /// <summary>
    /// Gets or sets the last written playlist size observed for the list.
    /// </summary>
    public int LastPlaylistItemCount { get; set; }
}
