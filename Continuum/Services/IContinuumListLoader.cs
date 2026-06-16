using Continuum.Models;

namespace Continuum.Services;

/// <summary>
/// Loads manual Continuum list definitions.
/// </summary>
public interface IContinuumListLoader
{
    /// <summary>
    /// Loads all valid list definitions.
    /// </summary>
    Task<IReadOnlyList<ContinuumListDefinition>> LoadAllAsync(CancellationToken cancellationToken);
}
