namespace Continuum.Services;

/// <summary>
/// Raised when another refresh is already in progress.
/// </summary>
public sealed class ContinuumRefreshConflictException(string message) : InvalidOperationException(message);
