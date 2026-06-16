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
            PlaylistSuffix = SanitizePlaylistSuffix(configuration.PlaylistSuffix, configuration.PlaylistSuffixSeparator),
            PlaylistSuffixSeparator = configuration.PlaylistSuffixSeparator ?? " - ",
            CreatePlaylistsForDisabledUsers = configuration.CreatePlaylistsForDisabledUsers,
            IncludePartiallyWatched = configuration.IncludePartiallyWatched,
            IncludeUnwatched = configuration.IncludeUnwatched,
            PlaylistImagePaths = new Dictionary<string, string>(
                configuration.PlaylistImagePaths ?? new Dictionary<string, string>(),
                StringComparer.OrdinalIgnoreCase),
            EnabledLists = SanitizeEnabledLists(configuration.EnabledLists)
        };

        if (!sanitized.IncludePartiallyWatched && !sanitized.IncludeUnwatched)
        {
            sanitized.IncludePartiallyWatched = true;
            sanitized.IncludeUnwatched = true;
        }

        return sanitized;
    }

    private static string SanitizePlaylistSuffix(string? playlistSuffix, string? legacyPlaylistSuffixSeparator)
    {
        string sanitized = (playlistSuffix ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(sanitized))
        {
            return "- Continuum";
        }

        if (LooksLikeNewStyleSuffix(sanitized))
        {
            return sanitized;
        }

        string legacySeparator = legacyPlaylistSuffixSeparator ?? " - ";
        if (string.IsNullOrEmpty(legacySeparator))
        {
            return sanitized;
        }

        string trimmedLegacySeparator = legacySeparator.Trim();
        if (string.IsNullOrEmpty(trimmedLegacySeparator))
        {
            return sanitized;
        }

        return trimmedLegacySeparator + " " + sanitized;
    }

    private static bool LooksLikeNewStyleSuffix(string playlistSuffix)
    {
        char firstCharacter = playlistSuffix[0];
        return char.IsPunctuation(firstCharacter) || char.IsWhiteSpace(firstCharacter);
    }

    private static Dictionary<string, bool> SanitizeEnabledLists(Dictionary<string, bool>? enabledLists)
    {
        Dictionary<string, bool> sanitized = new(StringComparer.OrdinalIgnoreCase);

        if (enabledLists is null)
        {
            return sanitized;
        }

        foreach (KeyValuePair<string, bool> pair in enabledLists)
        {
            string slug = pair.Key.Trim();
            if (string.IsNullOrWhiteSpace(slug))
            {
                continue;
            }

            sanitized[slug] = pair.Value;
        }

        return sanitized;
    }
}
