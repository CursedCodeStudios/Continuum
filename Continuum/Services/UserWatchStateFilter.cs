using Continuum.Configuration;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Continuum.Services;

/// <summary>
/// Filters items by Jellyfin user playback state.
/// </summary>
public sealed class UserWatchStateFilter : IUserWatchStateFilter
{
    private readonly IUserDataManager _userDataManager;
    private readonly ILogger<UserWatchStateFilter> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserWatchStateFilter"/> class.
    /// </summary>
    public UserWatchStateFilter(IUserDataManager userDataManager, ILogger<UserWatchStateFilter> logger)
    {
        _userDataManager = userDataManager;
        _logger = logger;
    }

    /// <inheritdoc />
    public bool ShouldInclude(User user, BaseItem item, PluginConfiguration configuration)
    {
        PluginConfiguration sanitized = PluginConfigurationSanitizer.Sanitize(configuration);
        if (!configuration.IncludePartiallyWatched && !configuration.IncludeUnwatched)
        {
            _logger.LogWarning(
                "Continuum configuration disabled both partial and unwatched inclusion; both modes will be treated as enabled.");
        }

        UserItemData? userData = _userDataManager.GetUserData(user, item);
        WatchStateEvaluation evaluation = WatchStateEvaluation.FromPlayback(
            userData?.Played == true,
            userData?.PlaybackPositionTicks ?? 0);

        return WatchStateDecider.ShouldInclude(
            evaluation,
            sanitized.IncludePartiallyWatched,
            sanitized.IncludeUnwatched);
    }
}
