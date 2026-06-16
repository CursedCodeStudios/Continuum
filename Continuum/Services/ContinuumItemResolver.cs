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

        ResolvedContinuumEntry[] resolved = list.Items
            .OrderBy(item => item.Order)
            .Select(item => ResolveEntry(list, item, movies, episodes))
            .ToArray();

        return Task.FromResult<IReadOnlyList<ResolvedContinuumEntry>>(resolved);
    }

    private ResolvedContinuumEntry ResolveEntry(
        ContinuumListDefinition list,
        ContinuumListEntry entry,
        IReadOnlyList<Movie> movies,
        IReadOnlyList<Episode> episodes)
    {
        if (entry.JellyfinItemId is Guid exactId)
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
        IReadOnlyList<Episode> episodes)
    {
        BaseItem[] candidates = FilterByProviderId(episodes, "Tvdb", entry.Providers.TvdbEpisodeId);
        if (TryResolveSingle(list, entry, candidates, "TVDb episode id", out ResolvedContinuumEntry resolved))
        {
            return resolved;
        }

        candidates = FilterByProviderId(episodes, "Tmdb", entry.Providers.TmdbEpisodeId);
        if (TryResolveSingle(list, entry, candidates, "TMDb episode id", out resolved))
        {
            return resolved;
        }

        candidates = episodes
            .Where(episode => EpisodeMatchesSeriesProvider(episode, "Tvdb", entry.Providers.TvdbSeriesId))
            .Where(episode => EpisodeMatchesNumbers(episode, entry))
            .Cast<BaseItem>()
            .ToArray();
        if (TryResolveSingle(list, entry, candidates, "TVDb series id + season/episode", out resolved))
        {
            return resolved;
        }

        candidates = episodes
            .Where(episode => EpisodeMatchesSeriesProvider(episode, "Tmdb", entry.Providers.TmdbSeriesId))
            .Where(episode => EpisodeMatchesNumbers(episode, entry))
            .Cast<BaseItem>()
            .ToArray();
        if (TryResolveSingle(list, entry, candidates, "TMDb series id + season/episode", out resolved))
        {
            return resolved;
        }

        if (!string.IsNullOrWhiteSpace(entry.Title))
        {
            candidates = episodes
                .Where(episode => string.Equals(episode.Name, entry.Title, StringComparison.OrdinalIgnoreCase))
                .Where(episode => !entry.SeasonNumber.HasValue || episode.ParentIndexNumber == entry.SeasonNumber)
                .Where(episode => !entry.EpisodeNumber.HasValue || episode.IndexNumber == entry.EpisodeNumber)
                .Cast<BaseItem>()
                .ToArray();

            if (TryResolveSingle(list, entry, candidates, "episode title fallback", out resolved))
            {
                return resolved;
            }
        }

        return Missing(list, entry, "Episode was not found in the library.");
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
