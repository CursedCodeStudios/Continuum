namespace Continuum.Services;

/// <summary>
/// Safety rules for destructive playlist updates.
/// </summary>
public static class PlaylistSafety
{
    /// <summary>
    /// Returns true when an empty refresh result should not replace an existing playlist.
    /// </summary>
    public static bool ShouldSkipUpdate(int newItemCount, int currentPlaylistItemCount, int previousRecordedItemCount)
    {
        if (newItemCount > 0)
        {
            return false;
        }

        return currentPlaylistItemCount > 0 || previousRecordedItemCount > 0;
    }
}
