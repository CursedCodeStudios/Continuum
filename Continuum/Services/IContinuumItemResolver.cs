using Continuum.Models;

namespace Continuum.Services;

/// <summary>
/// Resolves Continuum list entries to Jellyfin library items.
/// </summary>
public interface IContinuumItemResolver
{
    /// <summary>
    /// Resolves a list to Jellyfin items in canonical order.
    /// </summary>
    Task<IReadOnlyList<ResolvedContinuumEntry>> ResolveAsync(
        ContinuumListDefinition list,
        CancellationToken cancellationToken);
}
