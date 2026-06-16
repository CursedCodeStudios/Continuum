using Continuum.Services;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using Microsoft.Extensions.Logging;

namespace Continuum;

/// <summary>
/// Provides shared Continuum runtime services when host DI does not create plugin-scoped registrations.
/// </summary>
public static class ContinuumRuntime
{
    private static readonly object Sync = new();
    private static IContinuumItemResolver? _itemResolver;
    private static IContinuumRefreshService? _refreshService;

    /// <summary>
    /// Gets the shared item resolver instance.
    /// </summary>
    public static IContinuumItemResolver GetItemResolver(
        ILibraryManager libraryManager,
        ILoggerFactory loggerFactory)
    {
        lock (Sync)
        {
            if (_itemResolver is not null)
            {
                return _itemResolver;
            }

            _itemResolver = new ContinuumItemResolver(
                libraryManager,
                loggerFactory.CreateLogger<ContinuumItemResolver>());

            return _itemResolver;
        }
    }

    /// <summary>
    /// Gets the shared refresh service instance.
    /// </summary>
    public static IContinuumRefreshService GetRefreshService(
        IApplicationPaths applicationPaths,
        ILibraryManager libraryManager,
        IUserManager userManager,
        IUserDataManager userDataManager,
        IPlaylistManager playlistManager,
        ILoggerFactory loggerFactory)
    {
        lock (Sync)
        {
            if (_refreshService is not null)
            {
                return _refreshService;
            }

            IContinuumItemResolver itemResolver = GetItemResolver(libraryManager, loggerFactory);
            IContinuumListLoader listLoader = new ContinuumListLoader(
                applicationPaths,
                loggerFactory.CreateLogger<ContinuumListLoader>());
            IUserWatchStateFilter watchStateFilter = new UserWatchStateFilter(
                userDataManager,
                loggerFactory.CreateLogger<UserWatchStateFilter>());
            IContinuumPlaylistService playlistService = new ContinuumPlaylistService(
                playlistManager,
                loggerFactory.CreateLogger<ContinuumPlaylistService>());
            IContinuumStateStore stateStore = new ContinuumStateStore(
                applicationPaths,
                loggerFactory.CreateLogger<ContinuumStateStore>());

            _refreshService = new ContinuumRefreshService(
                listLoader,
                itemResolver,
                watchStateFilter,
                playlistService,
                stateStore,
                userManager,
                loggerFactory.CreateLogger<ContinuumRefreshService>());

            return _refreshService;
        }
    }
}
