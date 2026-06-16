using Continuum.Configuration;

namespace Continuum.Tests;

public class PluginConfigurationSanitizerTests
{
    [Fact]
    public void Sanitize_ClampsPlaylistSizeAndRefreshInterval()
    {
        PluginConfiguration configuration = new PluginConfiguration
        {
            PlaylistSize = 999,
            RefreshIntervalMinutes = 1
        };

        PluginConfiguration sanitized = PluginConfigurationSanitizer.Sanitize(configuration);

        Assert.Equal(150, sanitized.PlaylistSize);
        Assert.Equal(5, sanitized.RefreshIntervalMinutes);
    }

    [Fact]
    public void Sanitize_EnablesBothWatchStatesWhenBothAreDisabled()
    {
        PluginConfiguration configuration = new PluginConfiguration
        {
            IncludePartiallyWatched = false,
            IncludeUnwatched = false
        };

        PluginConfiguration sanitized = PluginConfigurationSanitizer.Sanitize(configuration);

        Assert.True(sanitized.IncludePartiallyWatched);
        Assert.True(sanitized.IncludeUnwatched);
    }
}
