namespace Continuum.Services;

/// <summary>
/// Result of resolving an explicit Jellyfin item id for diagnostic episode matching.
/// </summary>
internal sealed class ContinuumEpisodeResolverExactLookupResult
{
    public ContinuumEpisodeResolverCandidate? Candidate { get; init; }

    public bool IsIncompatibleType { get; init; }
}
