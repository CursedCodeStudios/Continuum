using MediaBrowser.Controller.Entities;

namespace Continuum.Services;

/// <summary>
/// Internal projected episode data used by resolver diagnostics.
/// </summary>
internal sealed class ContinuumEpisodeResolverCandidate
{
    public BaseItem? Item { get; init; }

    public Guid JellyfinItemId { get; init; }

    public string EpisodeTitle { get; init; } = string.Empty;

    public string? SeriesTitle { get; init; }

    public int? SeasonNumber { get; init; }

    public int? EpisodeNumber { get; init; }

    public int? ProductionYear { get; init; }

    public IReadOnlyDictionary<string, string> EpisodeProviderIds { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, string> SeriesProviderIds { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
