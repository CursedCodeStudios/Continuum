using Continuum.Configuration;
using Continuum.Models;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Entities;

namespace Continuum.Services;

/// <summary>
/// Creates and updates per-user Continuum playlists.
/// </summary>
public interface IContinuumPlaylistService
{
    /// <summary>
    /// Creates or updates a playlist for the provided list and user.
    /// </summary>
    Task<ContinuumPlaylistUpdateResult> CreateOrUpdateAsync(
        ContinuumListDefinition list,
        User user,
        IReadOnlyList<BaseItem> items,
        PluginConfiguration configuration,
        ContinuumUserPlaylistState? existingState,
        CancellationToken cancellationToken);
}
