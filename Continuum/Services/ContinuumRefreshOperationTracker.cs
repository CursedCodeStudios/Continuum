using Continuum.Models;

namespace Continuum.Services;

/// <summary>
/// Tracks the currently active or most recently completed refresh operation.
/// </summary>
internal sealed class ContinuumRefreshOperationTracker(Func<DateTimeOffset>? utcNow = null)
{
    private readonly object _sync = new();
    private readonly Func<DateTimeOffset> _utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);
    private ContinuumRefreshOperationStatus _status = ContinuumRefreshOperationStatus.CreateIdle();

    public ContinuumRefreshOperationStatus GetSnapshot()
    {
        lock (_sync)
        {
            return _status.Clone();
        }
    }

    public void Start(string? targetSlug)
    {
        lock (_sync)
        {
            if (string.Equals(_status.State, "running", StringComparison.OrdinalIgnoreCase))
            {
                throw new ContinuumRefreshConflictException("Another Continuum refresh is already running. Wait for it to finish before starting a new one.");
            }

            _status = new ContinuumRefreshOperationStatus
            {
                State = "running",
                Scope = string.IsNullOrWhiteSpace(targetSlug) ? "all-lists" : "single-list",
                TargetSlug = string.IsNullOrWhiteSpace(targetSlug) ? null : targetSlug,
                CurrentListSlug = string.IsNullOrWhiteSpace(targetSlug) ? null : targetSlug,
                StartedAtUtc = _utcNow(),
                CompletedAtUtc = null,
                ProcessedListCount = 0,
                TotalListCount = 0,
                ProcessedUserOperationCount = 0,
                TotalUserOperationCount = 0,
                Message = null
            };
        }
    }

    public void SetPlan(int totalListCount, int totalUserOperationCount)
    {
        lock (_sync)
        {
            _status.TotalListCount = totalListCount;
            _status.TotalUserOperationCount = totalUserOperationCount;
        }
    }

    public void Advance(string? currentListSlug, int processedListCount, int processedUserOperationCount)
    {
        lock (_sync)
        {
            _status.CurrentListSlug = currentListSlug;
            _status.ProcessedListCount = processedListCount;
            _status.ProcessedUserOperationCount = processedUserOperationCount;
        }
    }

    public void Complete(ContinuumRefreshResult result)
    {
        lock (_sync)
        {
            _status.State = "completed";
            _status.CompletedAtUtc = result.CompletedAtUtc;
            _status.CurrentListSlug = null;
            _status.ProcessedListCount = result.ListsProcessed;
            _status.TotalListCount = Math.Max(_status.TotalListCount, result.ListsProcessed);
            _status.Message = result.Warnings.LastOrDefault();
            _status.ProcessedUserOperationCount = Math.Max(_status.ProcessedUserOperationCount, _status.TotalUserOperationCount);
        }
    }

    public void Fail(Exception exception, string? messageOverride = null)
    {
        lock (_sync)
        {
            _status.State = "failed";
            _status.CompletedAtUtc = _utcNow();
            _status.CurrentListSlug = null;
            _status.Message = string.IsNullOrWhiteSpace(messageOverride) ? exception.Message : messageOverride;
        }
    }
}
