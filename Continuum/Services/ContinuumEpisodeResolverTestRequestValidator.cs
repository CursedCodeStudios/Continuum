using Continuum.Models;

namespace Continuum.Services;

/// <summary>
/// Validates admin episode resolver test requests.
/// </summary>
internal static class ContinuumEpisodeResolverTestRequestValidator
{
    public static string? Validate(ContinuumEpisodeResolverTestRequest request)
    {
        if (request.SeasonNumber.HasValue && request.SeasonNumber.Value < 1)
        {
            return "Season number must be a positive integer when provided.";
        }

        if (request.EpisodeNumber.HasValue && request.EpisodeNumber.Value < 1)
        {
            return "Episode number must be a positive integer when provided.";
        }

        bool hasMeaningfulHint = request.JellyfinItemId.HasValue
            || !string.IsNullOrWhiteSpace(request.TvdbSeriesId)
            || !string.IsNullOrWhiteSpace(request.TvdbEpisodeId)
            || !string.IsNullOrWhiteSpace(request.TmdbSeriesId)
            || !string.IsNullOrWhiteSpace(request.TmdbEpisodeId)
            || !string.IsNullOrWhiteSpace(request.Title);

        if (!hasMeaningfulHint)
        {
            return "Provide at least one resolver hint such as a Jellyfin id, TVDb/TMDb id, or title.";
        }

        return null;
    }
}
