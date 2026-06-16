using MediaBrowser.Model.Plugins;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace Continuum.Configuration;

/// <summary>
/// Plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    private Dictionary<string, bool> _enabledLists = new(StringComparer.OrdinalIgnoreCase);

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
    public Dictionary<string, bool> EnabledLists
    {
        get => _enabledLists;
        set => _enabledLists = NormalizeEnabledLists(value);
    }

    /// <summary>
    /// Gets or sets the XML-persisted per-list enabled state.
    /// </summary>
    [JsonIgnore]
    [XmlArray("EnabledLists")]
    [XmlArrayItem("List")]
    public List<ContinuumEnabledListSetting> PersistedEnabledLists
    {
        get => _enabledLists
            .Select(pair => new ContinuumEnabledListSetting
            {
                Slug = pair.Key,
                Enabled = pair.Value
            })
            .ToList();
        set => _enabledLists = NormalizeEnabledLists(value);
    }

    private static Dictionary<string, bool> NormalizeEnabledLists(IEnumerable<ContinuumEnabledListSetting>? enabledLists)
    {
        Dictionary<string, bool> normalized = new(StringComparer.OrdinalIgnoreCase);

        if (enabledLists is null)
        {
            return normalized;
        }

        foreach (ContinuumEnabledListSetting setting in enabledLists)
        {
            string slug = (setting.Slug ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(slug))
            {
                continue;
            }

            normalized[slug] = setting.Enabled;
        }

        return normalized;
    }

    private static Dictionary<string, bool> NormalizeEnabledLists(Dictionary<string, bool>? enabledLists)
    {
        Dictionary<string, bool> normalized = new(StringComparer.OrdinalIgnoreCase);

        if (enabledLists is null)
        {
            return normalized;
        }

        foreach (KeyValuePair<string, bool> pair in enabledLists)
        {
            string slug = pair.Key.Trim();
            if (string.IsNullOrWhiteSpace(slug))
            {
                continue;
            }

            normalized[slug] = pair.Value;
        }

        return normalized;
    }
}
