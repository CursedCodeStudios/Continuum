using Continuum.Models;

namespace Continuum.Services;

/// <summary>
/// Persists Continuum state.
/// </summary>
public interface IContinuumStateStore
{
    /// <summary>
    /// Loads the current state.
    /// </summary>
    Task<ContinuumState> LoadAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Saves the current state.
    /// </summary>
    Task SaveAsync(ContinuumState state, CancellationToken cancellationToken);
}
