namespace Continuum.Services;

/// <summary>
/// Represents a user's watch state for an item.
/// </summary>
public readonly record struct WatchStateEvaluation(bool IsPlayed, bool IsPartiallyWatched, bool IsUnwatched)
{
    /// <summary>
    /// Creates an evaluation from Jellyfin playback data.
    /// </summary>
    public static WatchStateEvaluation FromPlayback(bool isPlayed, long playbackPositionTicks)
    {
        bool isPartiallyWatched = !isPlayed && playbackPositionTicks > 0;
        bool isUnwatched = !isPlayed && playbackPositionTicks <= 0;
        return new WatchStateEvaluation(isPlayed, isPartiallyWatched, isUnwatched);
    }
}
