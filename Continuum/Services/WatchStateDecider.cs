namespace Continuum.Services;

/// <summary>
/// Pure watch-state eligibility rules used by the user filter.
/// </summary>
public static class WatchStateDecider
{
    /// <summary>
    /// Returns true when an item should be included.
    /// </summary>
    public static bool ShouldInclude(
        WatchStateEvaluation evaluation,
        bool includePartiallyWatched,
        bool includeUnwatched)
    {
        if (!includePartiallyWatched && !includeUnwatched)
        {
            includePartiallyWatched = true;
            includeUnwatched = true;
        }

        return
            !evaluation.IsPlayed
            && ((evaluation.IsPartiallyWatched && includePartiallyWatched)
                || (evaluation.IsUnwatched && includeUnwatched));
    }
}
