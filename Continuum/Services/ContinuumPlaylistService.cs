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
        string playlistName = BuildDefaultPlaylistName(list.Name, sanitized.PlaylistSuffix);
        string fallbackPlaylistName = BuildUserQualifiedPlaylistName(list.Name, ReflectionHelpers.GetUserName(user), sanitized.PlaylistSuffix);
        Playlist? playlist = FindExistingPlaylist(user, playlistName, fallbackPlaylistName, existingState?.PlaylistId);
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
                ?? FindExistingPlaylist(user, fallbackPlaylistName, null);

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
        return FindExistingPlaylist(user, playlistName, fallbackPlaylistName: null, playlistId);
    }

    private Playlist? FindExistingPlaylist(User user, string playlistName, string? fallbackPlaylistName, Guid? playlistId)
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

        if (string.IsNullOrWhiteSpace(fallbackPlaylistName))
        {
            return null;
        }

        return availablePlaylists.FirstOrDefault(playlist =>
            string.Equals(playlist.Name, fallbackPlaylistName, StringComparison.OrdinalIgnoreCase)
            && playlist.OwnerUserId == user.Id);
    }

    internal static string BuildDefaultPlaylistName(string listName, string playlistSuffix)
    {
        return string.IsNullOrWhiteSpace(playlistSuffix)
            ? listName
            : listName + " " + playlistSuffix.Trim();
    }

    internal static string BuildUserQualifiedPlaylistName(string listName, string userName, string playlistSuffix)
    {
        return BuildDefaultPlaylistName($"{listName} - {userName}", playlistSuffix);
    }
}
