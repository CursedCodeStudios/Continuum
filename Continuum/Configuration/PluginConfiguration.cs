using MediaBrowser.Model.Plugins;
using System.Xml.Serialization;

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
    /// Gets or sets the suffix appended to generated playlist names.
    /// </summary>
    public string PlaylistSuffix { get; set; } = "- Continuum";

    /// <summary>
    /// Gets or sets the legacy separator inserted before the playlist suffix.
    /// </summary>
    public string PlaylistSuffixSeparator { get; set; } = " - ";

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
    [XmlIgnore]
    public Dictionary<string, string> PlaylistImagePaths { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets the per-list enabled state keyed by list slug.
    /// </summary>
    [XmlIgnore]
    public Dictionary<string, bool> EnabledLists { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
