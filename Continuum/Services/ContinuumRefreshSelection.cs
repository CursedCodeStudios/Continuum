using Continuum.Models;

namespace Continuum.Services;

/// <summary>
/// Selects the enabled lists that should participate in a refresh.
/// </summary>
internal static class ContinuumRefreshSelection
{
    public static ContinuumListDefinition[] GetTargetLists(
        IReadOnlyList<ContinuumListDefinition> loadedLists,
        string? targetSlug)
    {
        ContinuumListDefinition[] lists = loadedLists
            .Where(list => list.Enabled)
            .Where(list => string.IsNullOrWhiteSpace(targetSlug)
                || string.Equals(list.Slug, targetSlug, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (!string.IsNullOrWhiteSpace(targetSlug) && lists.Length == 0)
        {
            throw new KeyNotFoundException($"No enabled Continuum list with slug '{targetSlug}' was found.");
        }

        return lists;
    }
}
