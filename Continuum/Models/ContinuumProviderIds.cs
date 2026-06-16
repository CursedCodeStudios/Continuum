namespace Continuum.Models;

/// <summary>
/// Known provider ids that can be used to match items.
/// </summary>
public sealed class ContinuumProviderIds
{
    /// <summary>
    /// Gets or sets the IMDb id.
    /// </summary>
    public string? ImdbId { get; set; }

    /// <summary>
    /// Gets or sets the TMDb movie id.
    /// </summary>
    public string? TmdbMovieId { get; set; }

    /// <summary>
    /// Gets or sets the TMDb series id.
    /// </summary>
    public string? TmdbSeriesId { get; set; }

    /// <summary>
    /// Gets or sets the TMDb episode id.
    /// </summary>
    public string? TmdbEpisodeId { get; set; }

    /// <summary>
    /// Gets or sets the TVDb series id.
    /// </summary>
    public string? TvdbSeriesId { get; set; }

    /// <summary>
    /// Gets or sets the TVDb episode id.
    /// </summary>
    public string? TvdbEpisodeId { get; set; }
}
