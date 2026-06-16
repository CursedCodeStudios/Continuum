using Continuum.Configuration;
using Continuum.Models;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Continuum.Services;

/// <summary>
/// Coordinates full Continuum refresh runs.
/// </summary>
public sealed class ContinuumRefreshService : IContinuumRefreshService
{
    private readonly IContinuumListLoader _listLoader;
    private readonly IContinuumItemResolver _itemResolver;
    private readonly IUserWatchStateFilter _watchStateFilter;
    private readonly IContinuumPlaylistService _playlistService;
    private readonly IContinuumStateStore _stateStore;
    private readonly IUserManager _userManager;
    private readonly ILogger<ContinuumRefreshService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContinuumRefreshService"/> class.
    /// </summary>
    public ContinuumRefreshService(
        IContinuumListLoader listLoader,
        IContinuumItemResolver itemResolver,
        IUserWatchStateFilter watchStateFilter,
        IContinuumPlaylistService playlistService,
        IContinuumStateStore stateStore,
        IUserManager userManager,
        ILogger<ContinuumRefreshService> logger)
    {
        _listLoader = listLoader;
        _itemResolver = itemResolver;
        _watchStateFilter = watchStateFilter;
        _playlistService = playlistService;
        _stateStore = stateStore;
        _userManager = userManager;
        _logger = logger;
    }

    /// <inheritdoc />
    public ContinuumRefreshResult? LastResult { get; private set; }

    /// <inheritdoc />
    public async Task<ContinuumRefreshResult> RefreshAllAsync(
        IProgress<double>? progress,
        CancellationToken cancellationToken)
    {
        DateTimeOffset startedAt = DateTimeOffset.UtcNow;
        PluginConfiguration? rawConfiguration = Plugin.Instance?.Configuration;
        PluginConfiguration configuration = PluginConfigurationSanitizer.Sanitize(rawConfiguration);
        ContinuumRefreshResult result = new ContinuumRefreshResult
        {
            StartedAtUtc = startedAt
        };

        if (rawConfiguration is not null
            && !rawConfiguration.IncludePartiallyWatched
            && !rawConfiguration.IncludeUnwatched)
        {
            result.Warnings.Add("Both IncludePartiallyWatched and IncludeUnwatched were disabled; Continuum treated both as enabled.");
        }

        if (!configuration.Enabled)
        {
            _logger.LogInformation("Continuum refresh skipped because the plugin is disabled.");
            result.CompletedAtUtc = DateTimeOffset.UtcNow;
            LastResult = result;
            return result;
        }

        ContinuumListDefinition[] lists = (await _listLoader.LoadAllAsync(cancellationToken).ConfigureAwait(false))
            .Where(list => list.Enabled)
            .ToArray();
        User[] users = _userManager.Users.OfType<User>().ToArray();
        ContinuumState state = await _stateStore.LoadAsync(cancellationToken).ConfigureAwait(false);
        User[] enabledUsers = configuration.CreatePlaylistsForDisabledUsers
            ? users
            : users.Where(user => !ReflectionHelpers.IsDisabled(user)).ToArray();

        _logger.LogInformation(
            "Starting Continuum refresh for {ListCount} list(s) across {UserCount} user(s).",
            lists.Length,
            enabledUsers.Length);

        result.ListsProcessed = lists.Length;
        result.UsersProcessed = enabledUsers.Length;

        int totalUserOperations = Math.Max(1, lists.Length * Math.Max(1, enabledUsers.Length));
        int operationIndex = 0;

        foreach (ContinuumListDefinition list in lists)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ContinuumListDefinition orderedList = list;
            orderedList.Items = orderedList.Items.OrderBy(item => item.Order).ToList();

            IReadOnlyList<ResolvedContinuumEntry> resolvedEntries = await _itemResolver.ResolveAsync(orderedList, cancellationToken).ConfigureAwait(false);
            result.ItemsResolved += resolvedEntries.Count(entry => entry.IsResolved);
            result.ItemsMissing += resolvedEntries.Count(entry => entry.IsMissing);
            result.ItemsAmbiguous += resolvedEntries.Count(entry => entry.IsAmbiguous);

            foreach (ResolvedContinuumEntry entry in resolvedEntries.Where(entry => entry.IsMissing || entry.IsAmbiguous))
            {
                if (!string.IsNullOrWhiteSpace(entry.Message))
                {
                    result.Warnings.Add($"{list.Slug}#{entry.Source.Order}: {entry.Message}");
                }
            }

            ContinuumListState listState = GetOrCreateListState(state, list.Slug);

            foreach (User user in enabledUsers)
            {
                cancellationToken.ThrowIfCancellationRequested();

                BaseItem[] eligibleItems = resolvedEntries
                    .Where(entry => entry.Item is not null)
                    .Select(entry => entry.Item!)
                    .Where(item => _watchStateFilter.ShouldInclude(user, item, configuration))
                    .Take(configuration.PlaylistSize)
                    .ToArray();

                string userStateKey = user.Id.ToString("D");
                listState.Users.TryGetValue(userStateKey, out ContinuumUserPlaylistState? existingUserState);

                ContinuumPlaylistUpdateResult playlistUpdate = await _playlistService.CreateOrUpdateAsync(
                    list,
                    user,
                    eligibleItems,
                    configuration,
                    existingUserState,
                    cancellationToken).ConfigureAwait(false);

                if (!listState.Users.TryGetValue(userStateKey, out ContinuumUserPlaylistState? persistedUserState))
                {
                    persistedUserState = new ContinuumUserPlaylistState();
                    listState.Users[userStateKey] = persistedUserState;
                }

                persistedUserState.PlaylistId = playlistUpdate.PlaylistId ?? persistedUserState.PlaylistId;
                persistedUserState.LastItemCount = playlistUpdate.ItemCount;
                persistedUserState.LastRefreshUtc = DateTimeOffset.UtcNow;

                if (playlistUpdate.Created)
                {
                    result.PlaylistsCreated++;
                }

                if (playlistUpdate.Updated)
                {
                    result.PlaylistsUpdated++;
                }

                if (!string.IsNullOrWhiteSpace(playlistUpdate.Warning))
                {
                    result.Warnings.Add(playlistUpdate.Warning);
                }

                operationIndex++;
                progress?.Report(operationIndex / (double)totalUserOperations);
            }
        }

        await _stateStore.SaveAsync(state, cancellationToken).ConfigureAwait(false);
        result.CompletedAtUtc = DateTimeOffset.UtcNow;
        LastResult = result;

        _logger.LogInformation(
            "Completed Continuum refresh in {DurationMs} ms. Created {CreatedCount} playlist(s), updated {UpdatedCount} playlist(s).",
            (result.CompletedAtUtc - result.StartedAtUtc).TotalMilliseconds,
            result.PlaylistsCreated,
            result.PlaylistsUpdated);

        return result;
    }

    private static ContinuumListState GetOrCreateListState(ContinuumState state, string slug)
    {
        if (!state.Lists.TryGetValue(slug, out ContinuumListState? listState))
        {
            listState = new ContinuumListState();
            state.Lists[slug] = listState;
        }

        return listState;
    }
}
