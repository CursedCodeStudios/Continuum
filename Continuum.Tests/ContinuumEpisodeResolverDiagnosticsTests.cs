using Continuum.Models;
using Continuum.Services;

namespace Continuum.Tests;

public sealed class ContinuumEpisodeResolverDiagnosticsTests
{
    [Fact]
    public void Resolve_UsesExactJellyfinItemIdWhenProvided()
    {
        Guid jellyfinItemId = Guid.NewGuid();
        ContinuumEpisodeResolverDiagnosticResult result = ContinuumEpisodeResolverDiagnostics.Resolve(
            CreateList(),
            CreateEntry(jellyfinItemId: jellyfinItemId),
            id => new ContinuumEpisodeResolverExactLookupResult
            {
                Candidate = CreateCandidate(jellyfinItemId, "Pilot", "Chicago Fire", 1, 1)
            },
            []);

        Assert.Equal("resolved", result.Response.Outcome);
        Assert.Equal("exact-jellyfin-id", result.Response.FinalStrategy);
        Assert.Single(result.Response.Trace);
        Assert.Equal("resolved", result.Response.Trace[0].Status);
        Assert.Equal("Pilot", result.Response.MatchedItem!.EpisodeTitle);
    }

    [Fact]
    public void Resolve_UsesTvdbEpisodeIdMatch()
    {
        ContinuumEpisodeResolverCandidate candidate = CreateCandidate(
            Guid.NewGuid(),
            "Pilot",
            "Chicago Fire",
            1,
            1,
            episodeProviderIds: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Tvdb"] = "tvdb-ep-1"
            });

        ContinuumEpisodeResolverDiagnosticResult result = ContinuumEpisodeResolverDiagnostics.Resolve(
            CreateList(),
            CreateEntry(tvdbEpisodeId: "tvdb-ep-1"),
            _ => new ContinuumEpisodeResolverExactLookupResult(),
            [candidate]);

        Assert.Equal("resolved", result.Response.Outcome);
        Assert.Equal("tvdb-episode-id", result.Response.FinalStrategy);
        Assert.Equal("skipped", result.Response.Trace[0].Status);
        Assert.Equal("resolved", result.Response.Trace[1].Status);
    }

    [Fact]
    public void Resolve_UsesTmdbEpisodeIdMatch()
    {
        ContinuumEpisodeResolverCandidate candidate = CreateCandidate(
            Guid.NewGuid(),
            "A Dark Day",
            "Chicago Fire",
            2,
            1,
            episodeProviderIds: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Tmdb"] = "tmdb-ep-2"
            });

        ContinuumEpisodeResolverDiagnosticResult result = ContinuumEpisodeResolverDiagnostics.Resolve(
            CreateList(),
            CreateEntry(tmdbEpisodeId: "tmdb-ep-2"),
            _ => new ContinuumEpisodeResolverExactLookupResult(),
            [candidate]);

        Assert.Equal("resolved", result.Response.Outcome);
        Assert.Equal("tmdb-episode-id", result.Response.FinalStrategy);
        Assert.Equal("skipped", result.Response.Trace[1].Status);
        Assert.Equal("resolved", result.Response.Trace[2].Status);
    }

    [Fact]
    public void Resolve_UsesSeriesIdAndEpisodeNumbers()
    {
        ContinuumEpisodeResolverCandidate candidate = CreateCandidate(
            Guid.NewGuid(),
            "Pilot",
            "Chicago Fire",
            1,
            1,
            seriesProviderIds: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Tvdb"] = "tvdb-series-1"
            });

        ContinuumEpisodeResolverDiagnosticResult result = ContinuumEpisodeResolverDiagnostics.Resolve(
            CreateList(),
            CreateEntry(tvdbSeriesId: "tvdb-series-1", seasonNumber: 1, episodeNumber: 1),
            _ => new ContinuumEpisodeResolverExactLookupResult(),
            [candidate]);

        Assert.Equal("resolved", result.Response.Outcome);
        Assert.Equal("tvdb-series-season-episode", result.Response.FinalStrategy);
        Assert.Equal("resolved", result.Response.Trace[3].Status);
    }

    [Fact]
    public void Resolve_SkipsStrategiesWithoutRequiredInputs()
    {
        ContinuumEpisodeResolverDiagnosticResult result = ContinuumEpisodeResolverDiagnostics.Resolve(
            CreateList(),
            CreateEntry(title: "Pilot"),
            _ => new ContinuumEpisodeResolverExactLookupResult(),
            []);

        Assert.Collection(
            result.Response.Trace,
            step => Assert.Equal("skipped", step.Status),
            step => Assert.Equal("skipped", step.Status),
            step => Assert.Equal("skipped", step.Status),
            step => Assert.Equal("skipped", step.Status),
            step => Assert.Equal("skipped", step.Status),
            step => Assert.Equal("no-match", step.Status));
    }

    [Fact]
    public void Resolve_AmbiguousResultIncludesCandidateSummaries()
    {
        ContinuumEpisodeResolverCandidate[] candidates =
        [
            CreateCandidate(Guid.NewGuid(), "Pilot", "Chicago Fire", 1, 1),
            CreateCandidate(Guid.NewGuid(), "Pilot", "Chicago P.D.", 1, 1)
        ];

        ContinuumEpisodeResolverDiagnosticResult result = ContinuumEpisodeResolverDiagnostics.Resolve(
            CreateList(),
            CreateEntry(title: "Pilot"),
            _ => new ContinuumEpisodeResolverExactLookupResult(),
            candidates);

        Assert.Equal("ambiguous", result.Response.Outcome);
        Assert.Equal("episode-title-fallback", result.Response.FinalStrategy);
        Assert.Equal("ambiguous", result.Response.Trace[^1].Status);
        Assert.Equal(2, result.Response.Trace[^1].Candidates.Count);
    }

    [Fact]
    public void Resolve_ReturnsMissingWhenNothingMatches()
    {
        ContinuumEpisodeResolverDiagnosticResult result = ContinuumEpisodeResolverDiagnostics.Resolve(
            CreateList(),
            CreateEntry(tvdbEpisodeId: "missing-tvdb-ep"),
            _ => new ContinuumEpisodeResolverExactLookupResult(),
            []);

        Assert.Equal("missing", result.Response.Outcome);
        Assert.Null(result.Response.FinalStrategy);
        Assert.Equal("Episode was not found in the library.", result.Response.Message);
    }

    private static ContinuumListDefinition CreateList()
    {
        return new ContinuumListDefinition
        {
            Name = "Resolver Test",
            Slug = "resolver-test"
        };
    }

    private static ContinuumListEntry CreateEntry(
        Guid? jellyfinItemId = null,
        string? tvdbSeriesId = null,
        string? tvdbEpisodeId = null,
        string? tmdbSeriesId = null,
        string? tmdbEpisodeId = null,
        string? title = null,
        int? seasonNumber = null,
        int? episodeNumber = null)
    {
        return new ContinuumListEntry
        {
            Order = 1,
            Type = ContinuumListEntryType.Episode,
            JellyfinItemId = jellyfinItemId,
            Title = title,
            SeasonNumber = seasonNumber,
            EpisodeNumber = episodeNumber,
            Providers = new ContinuumProviderIds
            {
                TvdbSeriesId = tvdbSeriesId,
                TvdbEpisodeId = tvdbEpisodeId,
                TmdbSeriesId = tmdbSeriesId,
                TmdbEpisodeId = tmdbEpisodeId
            }
        };
    }

    private static ContinuumEpisodeResolverCandidate CreateCandidate(
        Guid jellyfinItemId,
        string episodeTitle,
        string seriesTitle,
        int seasonNumber,
        int episodeNumber,
        IReadOnlyDictionary<string, string>? episodeProviderIds = null,
        IReadOnlyDictionary<string, string>? seriesProviderIds = null)
    {
        return new ContinuumEpisodeResolverCandidate
        {
            JellyfinItemId = jellyfinItemId,
            EpisodeTitle = episodeTitle,
            SeriesTitle = seriesTitle,
            SeasonNumber = seasonNumber,
            EpisodeNumber = episodeNumber,
            EpisodeProviderIds = episodeProviderIds ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            SeriesProviderIds = seriesProviderIds ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        };
    }
}
