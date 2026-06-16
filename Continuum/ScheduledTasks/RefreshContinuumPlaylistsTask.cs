using Continuum.Configuration;
using Continuum.Services;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Continuum.ScheduledTasks;

/// <summary>
/// Scheduled task that refreshes Continuum playlists.
/// </summary>
public sealed class RefreshContinuumPlaylistsTask : IScheduledTask
{
    private readonly ILogger<RefreshContinuumPlaylistsTask> _logger;
    private readonly IContinuumRefreshService _refreshService;

    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshContinuumPlaylistsTask"/> class.
    /// </summary>
    public RefreshContinuumPlaylistsTask(
        ILibraryManager libraryManager,
        IUserManager userManager,
        IUserDataManager userDataManager,
        IPlaylistManager playlistManager,
        IApplicationPaths applicationPaths,
        ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<RefreshContinuumPlaylistsTask>();

        ContinuumListLoader listLoader = new ContinuumListLoader(applicationPaths, loggerFactory.CreateLogger<ContinuumListLoader>());
        ContinuumItemResolver itemResolver = new ContinuumItemResolver(libraryManager, loggerFactory.CreateLogger<ContinuumItemResolver>());
        UserWatchStateFilter watchStateFilter = new UserWatchStateFilter(userDataManager, loggerFactory.CreateLogger<UserWatchStateFilter>());
        ContinuumPlaylistService playlistService = new ContinuumPlaylistService(playlistManager, loggerFactory.CreateLogger<ContinuumPlaylistService>());
        ContinuumStateStore stateStore = new ContinuumStateStore(applicationPaths, loggerFactory.CreateLogger<ContinuumStateStore>());

        _refreshService = new ContinuumRefreshService(
            listLoader,
            itemResolver,
            watchStateFilter,
            playlistService,
            stateStore,
            userManager,
            loggerFactory.CreateLogger<ContinuumRefreshService>());
    }

    /// <inheritdoc />
    public string Name => "Refresh Continuum Playlists";

    /// <inheritdoc />
    public string Key => "RefreshContinuumPlaylists";

    /// <inheritdoc />
    public string Description => "Refreshes generated Continuum playlists from manual ordered list files.";

    /// <inheritdoc />
    public string Category => "Continuum";

    /// <inheritdoc />
    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        PluginConfiguration configuration = PluginConfigurationSanitizer.Sanitize(Plugin.Instance?.Configuration);
        if (!configuration.Enabled)
        {
            _logger.LogInformation("Continuum scheduled task skipped because the plugin is disabled.");
            return;
        }

        _logger.LogInformation("Starting scheduled Continuum playlist refresh.");

        try
        {
            await _refreshService.RefreshAllAsync(progress, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Scheduled Continuum playlist refresh completed successfully.");
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Scheduled Continuum playlist refresh was canceled.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Scheduled Continuum playlist refresh failed.");
            throw;
        }
    }

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        // Jellyfin task triggers are static. Config changes may require a restart or manual task adjustment.
        return
        [
            new TaskTriggerInfo
            {
                Type = TaskTriggerInfoType.IntervalTrigger,
                IntervalTicks = TimeSpan.FromMinutes(60).Ticks
            }
        ];
    }
}
