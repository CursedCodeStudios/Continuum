using Continuum.Models;

namespace Continuum.Services;

/// <summary>
/// Shared episode resolver diagnostics that mirror Continuum's matching pipeline.
/// </summary>
internal static class ContinuumEpisodeResolverDiagnostics
{
    public static ContinuumEpisodeResolverDiagnosticResult Resolve(
        ContinuumListDefinition list,
        ContinuumListEntry entry,
        Func<Guid, ContinuumEpisodeResolverExactLookupResult> exactLookup,
        IReadOnlyList<ContinuumEpisodeResolverCandidate> episodes)
    {
        List<ContinuumEpisodeResolverTraceStep> trace = [];

        ContinuumEpisodeResolverDiagnosticResult? exactResult = TryResolveExactId(list, entry, exactLookup, trace);
        if (exactResult is not null)
        {
            return exactResult;
        }

        ContinuumEpisodeResolverDiagnosticResult? tvdbEpisodeResult = TryResolveByEpisodeProviderId(
            list,
            entry,
            trace,
            episodes,
            providerId: entry.Providers.TvdbEpisodeId,
            strategyKey: "tvdb-episode-id",
            displayName: "TVDb episode id",
            providerName: "Tvdb");
        if (tvdbEpisodeResult is not null)
        {
            return tvdbEpisodeResult;
        }

        ContinuumEpisodeResolverDiagnosticResult? tmdbEpisodeResult = TryResolveByEpisodeProviderId(
            list,
            entry,
            trace,
            episodes,
            providerId: entry.Providers.TmdbEpisodeId,
            strategyKey: "tmdb-episode-id",
            displayName: "TMDb episode id",
            providerName: "Tmdb");
        if (tmdbEpisodeResult is not null)
        {
            return tmdbEpisodeResult;
        }

        ContinuumEpisodeResolverDiagnosticResult? tvdbSeriesResult = TryResolveBySeriesAndNumbers(
            list,
            entry,
            trace,
            episodes,
            seriesProviderId: entry.Providers.TvdbSeriesId,
            strategyKey: "tvdb-series-season-episode",
            displayName: "TVDb series id + season/episode",
            providerName: "Tvdb");
        if (tvdbSeriesResult is not null)
        {
            return tvdbSeriesResult;
        }

        ContinuumEpisodeResolverDiagnosticResult? tmdbSeriesResult = TryResolveBySeriesAndNumbers(
            list,
            entry,
            trace,
            episodes,
            seriesProviderId: entry.Providers.TmdbSeriesId,
            strategyKey: "tmdb-series-season-episode",
            displayName: "TMDb series id + season/episode",
            providerName: "Tmdb");
        if (tmdbSeriesResult is not null)
        {
            return tmdbSeriesResult;
        }

        ContinuumEpisodeResolverDiagnosticResult? titleFallbackResult = TryResolveByTitleFallback(list, entry, trace, episodes);
        if (titleFallbackResult is not null)
        {
            return titleFallbackResult;
        }

        return new ContinuumEpisodeResolverDiagnosticResult
        {
            Response = new ContinuumEpisodeResolverTestResponse
            {
                Outcome = "missing",
                Message = "Episode was not found in the library.",
                Trace = trace
            }
        };
    }

    private static ContinuumEpisodeResolverDiagnosticResult? TryResolveExactId(
        ContinuumListDefinition list,
        ContinuumListEntry entry,
        Func<Guid, ContinuumEpisodeResolverExactLookupResult> exactLookup,
        ICollection<ContinuumEpisodeResolverTraceStep> trace)
    {
        if (!entry.JellyfinItemId.HasValue)
        {
            trace.Add(CreateStep(
                "exact-jellyfin-id",
                "Exact Jellyfin item id",
                "skipped",
                "Skipped because no Jellyfin item id was provided."));
            return null;
        }

        ContinuumEpisodeResolverExactLookupResult lookupResult = exactLookup(entry.JellyfinItemId.Value);
        if (lookupResult.Candidate is not null)
        {
            return CreateResolvedResult(
                "exact-jellyfin-id",
                "Exact Jellyfin item id",
                "Resolved by Jellyfin item id.",
                lookupResult.Candidate,
                trace);
        }

        string message = lookupResult.IsIncompatibleType
            ? "The provided Jellyfin item id resolved to a non-episode item."
            : "No episode was found for the provided Jellyfin item id.";
        trace.Add(CreateStep("exact-jellyfin-id", "Exact Jellyfin item id", "no-match", message));
        return null;
    }

    private static ContinuumEpisodeResolverDiagnosticResult? TryResolveByEpisodeProviderId(
        ContinuumListDefinition list,
        ContinuumListEntry entry,
        ICollection<ContinuumEpisodeResolverTraceStep> trace,
        IReadOnlyList<ContinuumEpisodeResolverCandidate> episodes,
        string? providerId,
        string strategyKey,
        string displayName,
        string providerName)
    {
        if (string.IsNullOrWhiteSpace(providerId))
        {
            trace.Add(CreateStep(strategyKey, displayName, "skipped", $"Skipped because no {displayName} was provided."));
            return null;
        }

        ContinuumEpisodeResolverCandidate[] candidates = episodes
            .Where(episode => TryGetProviderId(episode.EpisodeProviderIds, providerName, out string? value)
                && string.Equals(value, providerId, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        return BuildCandidateResult(strategyKey, displayName, candidates, trace);
    }

    private static ContinuumEpisodeResolverDiagnosticResult? TryResolveBySeriesAndNumbers(
        ContinuumListDefinition list,
        ContinuumListEntry entry,
        ICollection<ContinuumEpisodeResolverTraceStep> trace,
        IReadOnlyList<ContinuumEpisodeResolverCandidate> episodes,
        string? seriesProviderId,
        string strategyKey,
        string displayName,
        string providerName)
    {
        if (string.IsNullOrWhiteSpace(seriesProviderId))
        {
            trace.Add(CreateStep(strategyKey, displayName, "skipped", $"Skipped because no {displayName.Split(' ')[0]} series id was provided."));
            return null;
        }

        ContinuumEpisodeResolverCandidate[] candidates = episodes
            .Where(episode => TryGetProviderId(episode.SeriesProviderIds, providerName, out string? value)
                && string.Equals(value, seriesProviderId, StringComparison.OrdinalIgnoreCase))
            .Where(episode => EpisodeMatchesNumbers(episode, entry))
            .ToArray();

        return BuildCandidateResult(strategyKey, displayName, candidates, trace);
    }

    private static ContinuumEpisodeResolverDiagnosticResult? TryResolveByTitleFallback(
        ContinuumListDefinition list,
        ContinuumListEntry entry,
        ICollection<ContinuumEpisodeResolverTraceStep> trace,
        IReadOnlyList<ContinuumEpisodeResolverCandidate> episodes)
    {
        if (string.IsNullOrWhiteSpace(entry.Title))
        {
            trace.Add(CreateStep("episode-title-fallback", "Episode title fallback", "skipped", "Skipped because no title was provided."));
            return null;
        }

        ContinuumEpisodeResolverCandidate[] candidates = episodes
            .Where(episode => string.Equals(episode.EpisodeTitle, entry.Title, StringComparison.OrdinalIgnoreCase))
            .Where(episode => EpisodeMatchesNumbers(episode, entry))
            .ToArray();

        return BuildCandidateResult("episode-title-fallback", "Episode title fallback", candidates, trace);
    }

    private static ContinuumEpisodeResolverDiagnosticResult? BuildCandidateResult(
        string strategyKey,
        string displayName,
        IReadOnlyList<ContinuumEpisodeResolverCandidate> candidates,
        ICollection<ContinuumEpisodeResolverTraceStep> trace)
    {
        if (candidates.Count == 0)
        {
            trace.Add(CreateStep(strategyKey, displayName, "no-match", $"No library items matched by {displayName}."));
            return null;
        }

        if (candidates.Count == 1)
        {
            return CreateResolvedResult(
                strategyKey,
                displayName,
                $"Resolved by {displayName}.",
                candidates[0],
                trace);
        }

        IReadOnlyList<ContinuumEpisodeResolverItemSummary> summaries = candidates
            .Select(CreateItemSummary)
            .ToArray();
        trace.Add(CreateStep(
            strategyKey,
            displayName,
            "ambiguous",
            $"Multiple library items matched by {displayName}.",
            summaries));

        return new ContinuumEpisodeResolverDiagnosticResult
        {
            Response = new ContinuumEpisodeResolverTestResponse
            {
                Outcome = "ambiguous",
                FinalStrategy = strategyKey,
                Message = $"Multiple library items matched by {displayName}.",
                Trace = trace.ToArray()
            }
        };
    }

    private static ContinuumEpisodeResolverDiagnosticResult CreateResolvedResult(
        string strategyKey,
        string displayName,
        string message,
        ContinuumEpisodeResolverCandidate candidate,
        ICollection<ContinuumEpisodeResolverTraceStep> trace)
    {
        trace.Add(CreateStep(strategyKey, displayName, "resolved", message));

        return new ContinuumEpisodeResolverDiagnosticResult
        {
            Response = new ContinuumEpisodeResolverTestResponse
            {
                Outcome = "resolved",
                FinalStrategy = strategyKey,
                Message = message,
                MatchedItem = CreateItemSummary(candidate),
                Trace = trace.ToArray()
            },
            ResolvedCandidate = candidate
        };
    }

    private static ContinuumEpisodeResolverTraceStep CreateStep(
        string strategyKey,
        string displayName,
        string status,
        string message,
        IReadOnlyList<ContinuumEpisodeResolverItemSummary>? candidates = null)
    {
        return new ContinuumEpisodeResolverTraceStep
        {
            StrategyKey = strategyKey,
            DisplayName = displayName,
            Status = status,
            Message = message,
            Candidates = candidates ?? []
        };
    }

    private static ContinuumEpisodeResolverItemSummary CreateItemSummary(ContinuumEpisodeResolverCandidate candidate)
    {
        return new ContinuumEpisodeResolverItemSummary
        {
            JellyfinItemId = candidate.JellyfinItemId.ToString("D"),
            EpisodeTitle = candidate.EpisodeTitle,
            SeriesTitle = candidate.SeriesTitle,
            SeasonNumber = candidate.SeasonNumber,
            EpisodeNumber = candidate.EpisodeNumber,
            ProductionYear = candidate.ProductionYear
        };
    }

    private static bool EpisodeMatchesNumbers(ContinuumEpisodeResolverCandidate candidate, ContinuumListEntry entry)
    {
        return (!entry.SeasonNumber.HasValue || candidate.SeasonNumber == entry.SeasonNumber.Value)
            && (!entry.EpisodeNumber.HasValue || candidate.EpisodeNumber == entry.EpisodeNumber.Value);
    }

    private static bool TryGetProviderId(
        IReadOnlyDictionary<string, string> providerIds,
        string providerName,
        out string? providerId)
    {
        providerId = null;
        foreach (KeyValuePair<string, string> pair in providerIds)
        {
            if (string.Equals(pair.Key, providerName, StringComparison.OrdinalIgnoreCase))
            {
                providerId = pair.Value;
                return !string.IsNullOrWhiteSpace(providerId);
            }
        }

        return false;
    }
}
