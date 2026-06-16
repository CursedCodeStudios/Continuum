namespace Continuum.Configuration;

/// <summary>
/// Sanitizes configuration values before use.
/// </summary>
public static class PluginConfigurationSanitizer
{
    /// <summary>
    /// Returns a sanitized copy of the provided configuration.
    /// </summary>
    public static PluginConfiguration Sanitize(PluginConfiguration? configuration)
    {
        configuration ??= new PluginConfiguration();

        PluginConfiguration sanitized = new PluginConfiguration
        {
            Enabled = configuration.Enabled,
            RefreshIntervalMinutes = Math.Max(5, configuration.RefreshIntervalMinutes),
            PlaylistSize = Math.Clamp(configuration.PlaylistSize, 1, 150),
            CreatePlaylistsForDisabledUsers = configuration.CreatePlaylistsForDisabledUsers,
            IncludePartiallyWatched = configuration.IncludePartiallyWatched,
            IncludeUnwatched = configuration.IncludeUnwatched,
            PlaylistImagePaths = new Dictionary<string, string>(
                configuration.PlaylistImagePaths ?? new Dictionary<string, string>(),
                StringComparer.OrdinalIgnoreCase)
        };

        if (!sanitized.IncludePartiallyWatched && !sanitized.IncludeUnwatched)
        {
            sanitized.IncludePartiallyWatched = true;
            sanitized.IncludeUnwatched = true;
        }

        return sanitized;
    }
}
