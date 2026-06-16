namespace Continuum.Models;

/// <summary>
/// Lightweight summary of a Jellyfin episode match.
/// </summary>
public sealed class ContinuumEpisodeResolverItemSummary
{
    /// <summary>
    /// Gets or sets the Jellyfin item identifier.
    /// </summary>
    public string JellyfinItemId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the episode title.
    /// </summary>
    public string EpisodeTitle { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the series title.
    /// </summary>
    public string? SeriesTitle { get; set; }

    /// <summary>
    /// Gets or sets the season number.
    /// </summary>
    public int? SeasonNumber { get; set; }

    /// <summary>
    /// Gets or sets the episode number.
    /// </summary>
    public int? EpisodeNumber { get; set; }

    /// <summary>
    /// Gets or sets the production year.
    /// </summary>
    public int? ProductionYear { get; set; }
}
