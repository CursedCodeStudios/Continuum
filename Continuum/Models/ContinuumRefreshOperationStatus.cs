namespace Continuum.Models;

/// <summary>
/// Current status of a refresh operation for the admin UI.
/// </summary>
public sealed class ContinuumRefreshOperationStatus
{
    /// <summary>
    /// Gets or sets the current operation state.
    /// </summary>
    public string State { get; set; } = "idle";

    /// <summary>
    /// Gets or sets the operation scope.
    /// </summary>
    public string Scope { get; set; } = "all-lists";

    /// <summary>
    /// Gets or sets the targeted list slug for single-list refreshes.
    /// </summary>
    public string? TargetSlug { get; set; }

    /// <summary>
    /// Gets or sets the currently processing list slug.
    /// </summary>
    public string? CurrentListSlug { get; set; }

    /// <summary>
    /// Gets or sets when the current or last operation started.
    /// </summary>
    public DateTimeOffset? StartedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets when the current or last operation completed.
    /// </summary>
    public DateTimeOffset? CompletedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the processed list count.
    /// </summary>
    public int ProcessedListCount { get; set; }

    /// <summary>
    /// Gets or sets the total list count.
    /// </summary>
    public int TotalListCount { get; set; }

    /// <summary>
    /// Gets or sets the processed per-user playlist operations.
    /// </summary>
    public int ProcessedUserOperationCount { get; set; }

    /// <summary>
    /// Gets or sets the total per-user playlist operations.
    /// </summary>
    public int TotalUserOperationCount { get; set; }

    /// <summary>
    /// Gets or sets the latest warning or error message.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Creates an idle status payload.
    /// </summary>
    public static ContinuumRefreshOperationStatus CreateIdle()
    {
        return new ContinuumRefreshOperationStatus();
    }

    /// <summary>
    /// Creates a copy of the current status.
    /// </summary>
    public ContinuumRefreshOperationStatus Clone()
    {
        return new ContinuumRefreshOperationStatus
        {
            State = State,
            Scope = Scope,
            TargetSlug = TargetSlug,
            CurrentListSlug = CurrentListSlug,
            StartedAtUtc = StartedAtUtc,
            CompletedAtUtc = CompletedAtUtc,
            ProcessedListCount = ProcessedListCount,
            TotalListCount = TotalListCount,
            ProcessedUserOperationCount = ProcessedUserOperationCount,
            TotalUserOperationCount = TotalUserOperationCount,
            Message = Message
        };
    }
}
