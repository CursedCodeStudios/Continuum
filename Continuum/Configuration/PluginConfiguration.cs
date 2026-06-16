using MediaBrowser.Model.Plugins;

namespace Continuum.Configuration;

/// <summary>
/// Plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Gets or sets a value indicating whether the plugin is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the refresh interval in minutes.
    /// </summary>
    public int RefreshIntervalMinutes { get; set; } = 60;

    /// <summary>
    /// Gets or sets the generated playlist size.
    /// </summary>
    public int PlaylistSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets a value indicating whether disabled users should receive playlists.
    /// </summary>
    public bool CreatePlaylistsForDisabledUsers { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether partially watched items are eligible.
    /// </summary>
    public bool IncludePartiallyWatched { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether unwatched items are eligible.
    /// </summary>
    public bool IncludeUnwatched { get; set; } = true;

    /// <summary>
    /// Gets or sets optional playlist image paths keyed by list slug.
    /// </summary>
    public Dictionary<string, string> PlaylistImagePaths { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
