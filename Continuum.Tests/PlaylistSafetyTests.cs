using Continuum.Services;

namespace Continuum.Tests;

public class PlaylistSafetyTests
{
    [Theory]
    [InlineData(0, 10, 10, true)]
    [InlineData(0, 0, 5, true)]
    [InlineData(0, 0, 0, false)]
    [InlineData(3, 10, 10, false)]
    public void ShouldSkipUpdate_ProtectsExistingPlaylists(
        int newItemCount,
        int currentPlaylistItemCount,
        int previousRecordedItemCount,
        bool expected)
    {
        bool actual = PlaylistSafety.ShouldSkipUpdate(newItemCount, currentPlaylistItemCount, previousRecordedItemCount);

        Assert.Equal(expected, actual);
    }
}
