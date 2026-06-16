using Continuum.Models;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Continuum.Services;

/// <summary>
/// Resolves manual Continuum entries against the local Jellyfin library.
/// </summary>
public sealed class ContinuumItemResolver(ILibraryManager libraryManager, ILogger<ContinuumItemResolver> logger) : IContinuumItemResolver
{
    /// <inheritdoc />
    public Task<IReadOnlyList<ResolvedContinuumEntry>> ResolveAsync(
        ContinuumListDefinition list,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        BaseItem[] allItems = libraryManager.RootFolder.RecursiveChildren.OfType<BaseItem>().ToArray();
        Movie[] movies = allItems.OfType<Movie>().ToArray();
        Episode[] episodes = allItems.OfType<Episode>().ToArray();
        ContinuumEpisodeResolverCandidate[] episodeCandidates = episodes.Select(CreateEpisodeCandidate).ToArray();

        ResolvedContinuumEntry[] resolved = list.Items
            .OrderBy(item => item.Order)
            .Select(item => ResolveEntry(list, item, movies, episodeCandidates))
            .ToArray();

        return Task.FromResult<IReadOnlyList<ResolvedContinuumEntry>>(resolved);
    }

    /// <inheritdoc />
    public Task<ContinuumEpisodeResolverTestResponse> ResolveEpisodeTestAsync(
        ContinuumEpisodeResolverTestRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Episode[] episodes = libraryManager.RootFolder.RecursiveChildren.OfType<Episode>().ToArray();
        ContinuumEpisodeResolverCandidate[] episodeCandidates = episodes.Select(CreateEpisodeCandidate).ToArray();
        ContinuumListDefinition list = new()
        {
            Name = "Resolver Test",
            Slug = "resolver-test"
        };
        ContinuumListEntry entry = CreateEntryFromRequest(request);

        ContinuumEpisodeResolverDiagnosticResult diagnostic = ContinuumEpisodeResolverDiagnostics.Resolve(
            list,
            entry,
            LookupExactEpisodeCandidate,
            episodeCandidates);

        return Task.FromResult(diagnostic.Response);
    }

    private ResolvedContinuumEntry ResolveEntry(
        ContinuumListDefinition list,
        ContinuumListEntry entry,
        IReadOnlyList<Movie> movies,
        IReadOnlyList<ContinuumEpisodeResolverCandidate> episodes)
    {
        if (entry.Type != ContinuumListEntryType.Episode && entry.JellyfinItemId is Guid exactId)
        {
            BaseItem? exactItem = libraryManager.GetItemById(exactId);
            if (exactItem is not null && IsCompatibleType(entry.Type, exactItem))
            {
                return Resolved(list, entry, exactItem, "Resolved by Jellyfin item id.");
            }

            logger.LogWarning(
                "Continuum entry {ListSlug}/{Order} referenced Jellyfin item id {ItemId}, but the item was missing or incompatible.",
                list.Slug,
                entry.Order,
                exactId);
        }

        return entry.Type switch
        {
            ContinuumListEntryType.Movie => ResolveMovie(list, entry, movies),
            ContinuumListEntryType.Episode => ResolveEpisode(list, entry, episodes),
            _ => Missing(list, entry, "Unsupported entry type.")
        };
    }

    private ResolvedContinuumEntry ResolveMovie(
        ContinuumListDefinition list,
        ContinuumListEntry entry,
        IReadOnlyList<Movie> movies)
    {
        BaseItem[] candidates = FilterByProviderId(movies, "Tmdb", entry.Providers.TmdbMovieId);
        if (TryResolveSingle(list, entry, candidates, "TMDb movie id", out ResolvedContinuumEntry resolved))
        {
            return resolved;
        }

        candidates = FilterByProviderId(movies, "Imdb", entry.Providers.ImdbId);
        if (TryResolveSingle(list, entry, candidates, "IMDb id", out resolved))
        {
            return resolved;
        }

        if (!string.IsNullOrWhiteSpace(entry.Title))
        {
            candidates = movies
                .Where(movie => string.Equals(movie.Name, entry.Title, StringComparison.OrdinalIgnoreCase))
                .Where(movie => !entry.Year.HasValue || movie.ProductionYear == entry.Year.Value)
                .Cast<BaseItem>()
                .ToArray();

            if (TryResolveSingle(list, entry, candidates, "title/year", out resolved))
            {
                return resolved;
            }
        }

        return Missing(list, entry, "Movie was not found in the library.");
    }

    private ResolvedContinuumEntry ResolveEpisode(
        ContinuumListDefinition list,
        ContinuumListEntry entry,
        IReadOnlyList<ContinuumEpisodeResolverCandidate> episodes)
    {
        ContinuumEpisodeResolverDiagnosticResult diagnostic = ContinuumEpisodeResolverDiagnostics.Resolve(
            list,
            entry,
            LookupExactEpisodeCandidate,
            episodes);
        LogEpisodeDiagnostic(list, entry, diagnostic.Response);

        if (string.Equals(diagnostic.Response.Outcome, "resolved", StringComparison.OrdinalIgnoreCase)
            && diagnostic.ResolvedCandidate?.Item is not null)
        {
            return Resolved(list, entry, diagnostic.ResolvedCandidate.Item, diagnostic.Response.Message);
        }

        if (string.Equals(diagnostic.Response.Outcome, "ambiguous", StringComparison.OrdinalIgnoreCase))
        {
            return Ambiguous(list, entry, diagnostic.Response.Message);
        }

        return Missing(list, entry, diagnostic.Response.Message);
    }

    private static bool IsCompatibleType(ContinuumListEntryType type, BaseItem? item)
    {
        return (type, item) switch
        {
            (ContinuumListEntryType.Movie, Movie) => true,
            (ContinuumListEntryType.Episode, Episode) => true,
            _ => false
        };
    }

    private static bool EpisodeMatchesNumbers(Episode episode, ContinuumListEntry entry)
    {
        return (!entry.SeasonNumber.HasValue || episode.ParentIndexNumber == entry.SeasonNumber)
            && (!entry.EpisodeNumber.HasValue || episode.IndexNumber == entry.EpisodeNumber);
    }

    private static bool EpisodeMatchesSeriesProvider(Episode episode, string providerName, string? providerId)
    {
        if (string.IsNullOrWhiteSpace(providerId))
        {
            return false;
        }

        return TryGetProviderId(episode.Series, providerName, out string? seriesProviderId)
            && string.Equals(seriesProviderId, providerId, StringComparison.OrdinalIgnoreCase);
    }

    private static BaseItem[] FilterByProviderId<TItem>(IEnumerable<TItem> items, string providerName, string? providerId)
        where TItem : BaseItem
    {
        if (string.IsNullOrWhiteSpace(providerId))
        {
            return [];
        }

        return items
            .Where(item => TryGetProviderId(item, providerName, out string? value)
                && string.Equals(value, providerId, StringComparison.OrdinalIgnoreCase))
            .Cast<BaseItem>()
            .ToArray();
    }

    private static bool TryGetProviderId(BaseItem? item, string providerName, out string? providerId)
    {
        providerId = null;
        if (item?.ProviderIds is null)
        {
            return false;
        }

        foreach (KeyValuePair<string, string> pair in item.ProviderIds)
        {
            if (string.Equals(pair.Key, providerName, StringComparison.OrdinalIgnoreCase))
            {
                providerId = pair.Value;
                return !string.IsNullOrWhiteSpace(providerId);
            }
        }

        return false;
    }

    private static ContinuumListEntry CreateEntryFromRequest(ContinuumEpisodeResolverTestRequest request)
    {
        return new ContinuumListEntry
        {
            Order = 1,
            Type = ContinuumListEntryType.Episode,
            Title = request.Title,
            SeasonNumber = request.SeasonNumber,
            EpisodeNumber = request.EpisodeNumber,
            JellyfinItemId = request.JellyfinItemId,
            Providers = new ContinuumProviderIds
            {
                TvdbSeriesId = request.TvdbSeriesId,
                TvdbEpisodeId = request.TvdbEpisodeId,
                TmdbSeriesId = request.TmdbSeriesId,
                TmdbEpisodeId = request.TmdbEpisodeId
            }
        };
    }

    private ContinuumEpisodeResolverExactLookupResult LookupExactEpisodeCandidate(Guid exactId)
    {
        BaseItem? exactItem = libraryManager.GetItemById(exactId);
        if (exactItem is Episode episode)
        {
            return new ContinuumEpisodeResolverExactLookupResult
            {
                Candidate = CreateEpisodeCandidate(episode)
            };
        }

        return new ContinuumEpisodeResolverExactLookupResult
        {
            IsIncompatibleType = exactItem is not null
        };
    }

    private void LogEpisodeDiagnostic(
        ContinuumListDefinition list,
        ContinuumListEntry entry,
        ContinuumEpisodeResolverTestResponse response)
    {
        foreach (ContinuumEpisodeResolverTraceStep step in response.Trace)
        {
            if (string.Equals(step.StrategyKey, "exact-jellyfin-id", StringComparison.OrdinalIgnoreCase)
                && string.Equals(step.Status, "no-match", StringComparison.OrdinalIgnoreCase)
                && entry.JellyfinItemId.HasValue)
            {
                logger.LogWarning(
                    "Continuum entry {ListSlug}/{Order} referenced Jellyfin item id {ItemId}, but the item was missing or incompatible.",
                    list.Slug,
                    entry.Order,
                    entry.JellyfinItemId.Value);
            }

            if (string.Equals(step.Status, "ambiguous", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning(
                    "Continuum entry {ListSlug}/{Order} was ambiguous when matching by {Strategy}.",
                    list.Slug,
                    entry.Order,
                    step.DisplayName);
            }
        }
    }

    private static ContinuumEpisodeResolverCandidate CreateEpisodeCandidate(Episode episode)
    {
        return new ContinuumEpisodeResolverCandidate
        {
            Item = episode,
            JellyfinItemId = episode.Id,
            EpisodeTitle = episode.Name,
            SeriesTitle = episode.Series?.Name,
            SeasonNumber = episode.ParentIndexNumber,
            EpisodeNumber = episode.IndexNumber,
            ProductionYear = episode.ProductionYear,
            EpisodeProviderIds = ToProviderLookup(episode),
            SeriesProviderIds = ToProviderLookup(episode.Series)
        };
    }

    private static IReadOnlyDictionary<string, string> ToProviderLookup(BaseItem? item)
    {
        if (item?.ProviderIds is null)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        Dictionary<string, string> lookup = new(StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<string, string> pair in item.ProviderIds)
        {
            lookup[pair.Key] = pair.Value;
        }

        return lookup;
    }

    private bool TryResolveSingle(
        ContinuumListDefinition list,
        ContinuumListEntry entry,
        IReadOnlyList<BaseItem> candidates,
        string strategy,
        out ResolvedContinuumEntry resolved)
    {
        if (candidates.Count == 1)
        {
            resolved = Resolved(list, entry, candidates[0], $"Resolved by {strategy}.");
            return true;
        }

        if (candidates.Count > 1)
        {
            logger.LogWarning(
                "Continuum entry {ListSlug}/{Order} was ambiguous when matching by {Strategy}.",
                list.Slug,
                entry.Order,
                strategy);
            resolved = Ambiguous(list, entry, $"Multiple matches were found by {strategy}.");
            return true;
        }

        resolved = null!;
        return false;
    }

    private static ResolvedContinuumEntry Resolved(
        ContinuumListDefinition list,
        ContinuumListEntry entry,
        BaseItem item,
        string message)
        => new()
        {
            List = list,
            Source = entry,
            Item = item,
            Message = message
        };

    private static ResolvedContinuumEntry Missing(
        ContinuumListDefinition list,
        ContinuumListEntry entry,
        string message)
        => new()
        {
            List = list,
            Source = entry,
            IsMissing = true,
            Message = message
        };

    private static ResolvedContinuumEntry Ambiguous(
        ContinuumListDefinition list,
        ContinuumListEntry entry,
        string message)
        => new()
        {
            List = list,
            Source = entry,
            IsAmbiguous = true,
            Message = message
        };
}
