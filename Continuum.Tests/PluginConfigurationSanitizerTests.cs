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

    [Fact]
    public void Sanitize_DefaultsBlankPlaylistSuffixToContinuum()
    {
        PluginConfiguration configuration = new PluginConfiguration
        {
            PlaylistSuffix = "   "
        };

        PluginConfiguration sanitized = PluginConfigurationSanitizer.Sanitize(configuration);

        Assert.Equal("- Continuum", sanitized.PlaylistSuffix);
    }

    [Fact]
    public void Sanitize_TrimsPlaylistSuffix()
    {
        PluginConfiguration configuration = new PluginConfiguration
        {
            PlaylistSuffix = "  (Dynamic)  "
        };

        PluginConfiguration sanitized = PluginConfigurationSanitizer.Sanitize(configuration);

        Assert.Equal("(Dynamic)", sanitized.PlaylistSuffix);
    }

    [Fact]
    public void Sanitize_MigratesLegacySuffixUsingLegacySeparator()
    {
        PluginConfiguration configuration = new PluginConfiguration
        {
            PlaylistSuffix = "Continuum",
            PlaylistSuffixSeparator = " - "
        };

        PluginConfiguration sanitized = PluginConfigurationSanitizer.Sanitize(configuration);

        Assert.Equal("- Continuum", sanitized.PlaylistSuffix);
    }

    [Fact]
    public void Sanitize_PreservesPlainSuffixWhenLegacySeparatorWasBlank()
    {
        PluginConfiguration configuration = new PluginConfiguration
        {
            PlaylistSuffix = "RollingWatch",
            PlaylistSuffixSeparator = string.Empty
        };

        PluginConfiguration sanitized = PluginConfigurationSanitizer.Sanitize(configuration);

        Assert.Equal("RollingWatch", sanitized.PlaylistSuffix);
    }

    [Fact]
    public void Sanitize_TrimsEnabledListKeysAndDropsBlankKeys()
    {
        PluginConfiguration configuration = new PluginConfiguration
        {
            EnabledLists = new Dictionary<string, bool>
            {
                [" chicago-universe "] = true,
                ["   "] = true,
                ["marvel-cinematic-universe"] = false
            }
        };

        PluginConfiguration sanitized = PluginConfigurationSanitizer.Sanitize(configuration);

        Assert.True(sanitized.EnabledLists.TryGetValue("chicago-universe", out bool chicagoEnabled));
        Assert.True(chicagoEnabled);
        Assert.True(sanitized.EnabledLists.TryGetValue("marvel-cinematic-universe", out bool marvelEnabled));
        Assert.False(marvelEnabled);
        Assert.DoesNotContain("   ", sanitized.EnabledLists.Keys);
        Assert.DoesNotContain(string.Empty, sanitized.EnabledLists.Keys);
    }

    [Fact]
    public void PersistedEnabledLists_PopulatesRuntimeEnabledLists()
    {
        PluginConfiguration configuration = new PluginConfiguration
        {
            PersistedEnabledLists =
            [
                new ContinuumEnabledListSetting
                {
                    Slug = " chicago-universe ",
                    Enabled = true
                },
                new ContinuumEnabledListSetting
                {
                    Slug = "star-wars",
                    Enabled = false
                }
            ]
        };

        Assert.True(configuration.EnabledLists.TryGetValue("chicago-universe", out bool chicagoEnabled));
        Assert.True(chicagoEnabled);
        Assert.True(configuration.EnabledLists.TryGetValue("star-wars", out bool starWarsEnabled));
        Assert.False(starWarsEnabled);
    }

    [Fact]
    public void PersistedEnabledLists_ReflectsRuntimeEnabledLists()
    {
        PluginConfiguration configuration = new PluginConfiguration
        {
            EnabledLists = new Dictionary<string, bool>
            {
                ["chicago-universe"] = true,
                ["star-wars"] = false
            }
        };

        List<ContinuumEnabledListSetting> persisted = configuration.PersistedEnabledLists;

        Assert.Equal(2, persisted.Count);
        Assert.Contains(persisted, setting => setting.Slug == "chicago-universe" && setting.Enabled);
        Assert.Contains(persisted, setting => setting.Slug == "star-wars" && !setting.Enabled);
    }
}
