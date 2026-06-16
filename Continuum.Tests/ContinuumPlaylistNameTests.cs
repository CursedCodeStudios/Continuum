using Continuum.Services;

namespace Continuum.Tests;

public class ContinuumPlaylistNameTests
{
    [Fact]
    public void BuildDefaultPlaylistName_UsesConfiguredSuffix()
    {
        string playlistName = ContinuumPlaylistService.BuildDefaultPlaylistName("Chicago Universe", "Rolling Watch");

        Assert.Equal("Chicago Universe - Rolling Watch", playlistName);
    }

    [Fact]
    public void BuildUserQualifiedPlaylistName_UsesConfiguredSuffix()
    {
        string playlistName = ContinuumPlaylistService.BuildUserQualifiedPlaylistName("Chicago Universe", "derek", "Rolling Watch");

        Assert.Equal("Chicago Universe - derek - Rolling Watch", playlistName);
    }
}
