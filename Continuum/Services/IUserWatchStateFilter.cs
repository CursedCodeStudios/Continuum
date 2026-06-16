using Continuum.Configuration;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Entities;

namespace Continuum.Services;

/// <summary>
/// Filters items by a user's watch state.
/// </summary>
public interface IUserWatchStateFilter
{
    /// <summary>
    /// Returns true when the item should be included for the user.
    /// </summary>
    bool ShouldInclude(User user, BaseItem item, PluginConfiguration configuration);
}
