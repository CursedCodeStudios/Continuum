namespace Continuum.Models;

/// <summary>
/// A single manual list entry.
/// </summary>
public sealed class ContinuumListEntry
{
    /// <summary>
    /// Gets or sets the explicit order.
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Gets or sets the entry type.
    /// </summary>
    public ContinuumListEntryType Type { get; set; }

    /// <summary>
    /// Gets or sets the entry title.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the release year.
    /// </summary>
    public int? Year { get; set; }

    /// <summary>
    /// Gets or sets the season number.
    /// </summary>
    public int? SeasonNumber { get; set; }

    /// <summary>
    /// Gets or sets the episode number.
    /// </summary>
    public int? EpisodeNumber { get; set; }

    /// <summary>
    /// Gets or sets an explicit Jellyfin item id.
    /// </summary>
    public Guid? JellyfinItemId { get; set; }

    /// <summary>
    /// Gets or sets provider identifiers used for resolution.
    /// </summary>
    public ContinuumProviderIds Providers { get; set; } = new();
}
