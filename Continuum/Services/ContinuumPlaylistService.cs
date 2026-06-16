using Continuum.Configuration;
using Continuum.Models;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Model.Playlists;
using Microsoft.Extensions.Logging;

namespace Continuum.Services;

/// <summary>
/// Creates and updates user-specific Continuum playlists.
/// </summary>
public sealed class ContinuumPlaylistService : IContinuumPlaylistService
{
    private readonly IPlaylistManager _playlistManager;
    private readonly ILogger<ContinuumPlaylistService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContinuumPlaylistService"/> class.
    /// </summary>
    public ContinuumPlaylistService(IPlaylistManager playlistManager, ILogger<ContinuumPlaylistService> logger)
    {
        _playlistManager = playlistManager;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ContinuumPlaylistUpdateResult> CreateOrUpdateAsync(
        ContinuumListDefinition list,
        User user,
        IReadOnlyList<BaseItem> items,
        PluginConfiguration configuration,
        ContinuumUserPlaylistState? existingState,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        PluginConfiguration sanitized = PluginConfigurationSanitizer.Sanitize(configuration);
        string playlistName = BuildPlaylistName(list, user, preferUserQualifiedName: false);
        Playlist? playlist = FindExistingPlaylist(user, playlistName, existingState?.PlaylistId);
        int currentItemCount = playlist?.GetManageableItems().Count ?? 0;
        int previousRecordedItemCount = existingState?.LastItemCount ?? 0;

        if (PlaylistSafety.ShouldSkipUpdate(items.Count, currentItemCount, previousRecordedItemCount))
        {
            string warning = $"Skipped updating playlist '{playlistName}' for safety because zero eligible items were resolved.";
            _logger.LogWarning(
                "Skipping Continuum playlist update for list {ListSlug} and user {UserId}: zero eligible items were found, but the existing playlist contains items.",
                list.Slug,
                user.Id);

            return new ContinuumPlaylistUpdateResult
            {
                PlaylistId = playlist?.Id ?? existingState?.PlaylistId,
                SkippedForSafety = true,
                ItemCount = currentItemCount,
                Warning = warning
            };
        }

        Guid[] itemIds = items.Select(item => item.Id).ToArray();

        if (playlist is null)
        {
            await _playlistManager.CreatePlaylist(new PlaylistCreationRequest
            {
                Name = playlistName,
                ItemIdList = itemIds,
                UserId = user.Id,
                Public = false
            }).ConfigureAwait(false);

            playlist = FindExistingPlaylist(user, playlistName, null)
                ?? FindExistingPlaylist(user, BuildPlaylistName(list, user, preferUserQualifiedName: true), null);

            if (playlist is null)
            {
                throw new InvalidOperationException($"Continuum created playlist '{playlistName}', but it could not be reloaded.");
            }

            _logger.LogInformation(
                "Created Continuum playlist {PlaylistName} for user {UserId} with {ItemCount} item(s).",
                playlist.Name,
                user.Id,
                itemIds.Length);

            return new ContinuumPlaylistUpdateResult
            {
                Created = true,
                PlaylistId = playlist.Id,
                ItemCount = itemIds.Length
            };
        }

        await _playlistManager.UpdatePlaylist(new PlaylistUpdateRequest
        {
            Id = playlist.Id,
            UserId = user.Id,
            Name = playlist.Name,
            Ids = itemIds,
            Public = false
        }).ConfigureAwait(false);

        _logger.LogInformation(
            "Updated Continuum playlist {PlaylistName} for user {UserId} with {ItemCount} item(s).",
            playlist.Name,
            user.Id,
            itemIds.Length);

        return new ContinuumPlaylistUpdateResult
        {
            Updated = true,
            PlaylistId = playlist.Id,
            ItemCount = itemIds.Length
        };
    }

    private Playlist? FindExistingPlaylist(User user, string playlistName, Guid? playlistId)
    {
        Playlist[] availablePlaylists = _playlistManager.GetPlaylists(user.Id).ToArray();

        if (playlistId is Guid knownPlaylistId)
        {
            Playlist? byId = availablePlaylists.FirstOrDefault(playlist => playlist.Id == knownPlaylistId);
            if (byId is not null)
            {
                return byId;
            }
        }

        Playlist? byDefaultName = availablePlaylists.FirstOrDefault(playlist =>
            string.Equals(playlist.Name, playlistName, StringComparison.OrdinalIgnoreCase)
            && playlist.OwnerUserId == user.Id);
        if (byDefaultName is not null)
        {
            return byDefaultName;
        }

        string fallbackName = BuildPlaylistNameFromValues(playlistName, ReflectionHelpers.GetUserName(user));
        return availablePlaylists.FirstOrDefault(playlist =>
            string.Equals(playlist.Name, fallbackName, StringComparison.OrdinalIgnoreCase)
            && playlist.OwnerUserId == user.Id);
    }

    private static string BuildPlaylistName(ContinuumListDefinition list, User user, bool preferUserQualifiedName)
    {
        return preferUserQualifiedName
            ? BuildPlaylistNameFromValues(list.Name, ReflectionHelpers.GetUserName(user))
            : $"{list.Name} - Continuum";
    }

    private static string BuildPlaylistNameFromValues(string listName, string userName)
    {
        return $"{listName} - {userName} - Continuum";
    }
}
