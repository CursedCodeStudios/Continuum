using Continuum.Models;

namespace Continuum.Services;

/// <summary>
/// Internal diagnostic result that preserves the resolved library item when present.
/// </summary>
internal sealed class ContinuumEpisodeResolverDiagnosticResult
{
    public required ContinuumEpisodeResolverTestResponse Response { get; init; }

    public ContinuumEpisodeResolverCandidate? ResolvedCandidate { get; init; }
}
