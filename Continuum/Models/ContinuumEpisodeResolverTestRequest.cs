namespace Continuum.Models;

/// <summary>
/// One-off admin request used to test TV episode resolver inputs.
/// </summary>
public sealed class ContinuumEpisodeResolverTestRequest
{
    /// <summary>
    /// Gets or sets an explicit Jellyfin item id.
    /// </summary>
    public Guid? JellyfinItemId { get; set; }

    /// <summary>
    /// Gets or sets the TVDb series id.
    /// </summary>
    public string? TvdbSeriesId { get; set; }

    /// <summary>
    /// Gets or sets the TVDb episode id.
    /// </summary>
    public string? TvdbEpisodeId { get; set; }

    /// <summary>
    /// Gets or sets the TMDb series id.
    /// </summary>
    public string? TmdbSeriesId { get; set; }

    /// <summary>
    /// Gets or sets the TMDb episode id.
    /// </summary>
    public string? TmdbEpisodeId { get; set; }

    /// <summary>
    /// Gets or sets an optional episode title.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets an optional season number.
    /// </summary>
    public int? SeasonNumber { get; set; }

    /// <summary>
    /// Gets or sets an optional episode number.
    /// </summary>
    public int? EpisodeNumber { get; set; }
}
