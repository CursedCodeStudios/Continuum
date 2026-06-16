using Continuum.Configuration;
using Continuum.Models;

namespace Continuum.Services;

/// <summary>
/// Selects the enabled lists that should participate in a refresh.
/// </summary>
internal static class ContinuumRefreshSelection
{
    public static ContinuumListDefinition[] GetTargetLists(
        IReadOnlyList<ContinuumListDefinition> loadedLists,
        PluginConfiguration configuration,
        string? targetSlug)
    {
        ContinuumListDefinition[] lists = loadedLists
            .Where(list => IsListEnabled(configuration, list.Slug))
            .Where(list => string.IsNullOrWhiteSpace(targetSlug)
                || string.Equals(list.Slug, targetSlug, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (!string.IsNullOrWhiteSpace(targetSlug) && lists.Length == 0)
        {
            throw new KeyNotFoundException($"No enabled Continuum list with slug '{targetSlug}' was found.");
        }

        return lists;
    }

    public static bool IsListEnabled(PluginConfiguration configuration, string slug)
    {
        return configuration.EnabledLists.TryGetValue(slug, out bool enabled) && enabled;
    }
}
