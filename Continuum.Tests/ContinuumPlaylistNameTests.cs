using Continuum.Services;

namespace Continuum.Tests;

public class ContinuumPlaylistNameTests
{
    [Fact]
    public void BuildDefaultPlaylistName_UsesConfiguredSuffix()
    {
        string playlistName = ContinuumPlaylistService.BuildDefaultPlaylistName("Chicago Universe", "- Continuum");

        Assert.Equal("Chicago Universe - Continuum", playlistName);
    }

    [Fact]
    public void BuildUserQualifiedPlaylistName_UsesConfiguredSuffix()
    {
        string playlistName = ContinuumPlaylistService.BuildUserQualifiedPlaylistName("Chicago Universe", "derek", "- Continuum");

        Assert.Equal("Chicago Universe - derek - Continuum", playlistName);
    }

    [Fact]
    public void BuildDefaultPlaylistName_AllowsParentheticalSuffix()
    {
        string playlistName = ContinuumPlaylistService.BuildDefaultPlaylistName("Chicago Universe", "(Dynamic)");

        Assert.Equal("Chicago Universe (Dynamic)", playlistName);
    }

    [Fact]
    public void BuildDefaultPlaylistName_SkipsEmptySuffix()
    {
        string playlistName = ContinuumPlaylistService.BuildDefaultPlaylistName("Chicago Universe", string.Empty);

        Assert.Equal("Chicago Universe", playlistName);
    }
}
