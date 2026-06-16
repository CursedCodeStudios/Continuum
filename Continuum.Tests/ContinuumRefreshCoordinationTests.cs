using Continuum.Models;
using Continuum.Services;
using Continuum.Configuration;

namespace Continuum.Tests;

public sealed class ContinuumRefreshCoordinationTests
{
    [Fact]
    public void GetTargetLists_ReturnsOnlyListsEnabledInConfiguration()
    {
        ContinuumListDefinition[] loadedLists =
        [
            new ContinuumListDefinition
            {
                Name = "Chicago Universe",
                Slug = "chicago-universe"
            },
            new ContinuumListDefinition
            {
                Name = "Star Wars",
                Slug = "star-wars"
            }
        ];
        PluginConfiguration configuration = new PluginConfiguration
        {
            EnabledLists = new Dictionary<string, bool>
            {
                ["star-wars"] = true
            }
        };

        ContinuumListDefinition[] targetLists = ContinuumRefreshSelection.GetTargetLists(loadedLists, configuration, targetSlug: null);

        Assert.Single(targetLists);
        Assert.Equal("star-wars", targetLists[0].Slug);
    }

    [Fact]
    public void GetTargetLists_RejectsUnknownSlug()
    {
        ContinuumListDefinition[] loadedLists =
        [
            new ContinuumListDefinition
            {
                Name = "Chicago Universe",
                Slug = "chicago-universe"
            }
        ];
        PluginConfiguration configuration = new PluginConfiguration
        {
            EnabledLists = new Dictionary<string, bool>
            {
                ["chicago-universe"] = true
            }
        };

        KeyNotFoundException exception = Assert.Throws<KeyNotFoundException>(() =>
            ContinuumRefreshSelection.GetTargetLists(loadedLists, configuration, "missing-list"));

        Assert.Contains("missing-list", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void GetTargetLists_RejectsDisabledSlug()
    {
        ContinuumListDefinition[] loadedLists =
        [
            new ContinuumListDefinition
            {
                Name = "Chicago Universe",
                Slug = "chicago-universe"
            }
        ];
        PluginConfiguration configuration = new PluginConfiguration();

        KeyNotFoundException exception = Assert.Throws<KeyNotFoundException>(() =>
            ContinuumRefreshSelection.GetTargetLists(loadedLists, configuration, "chicago-universe"));

        Assert.Contains("chicago-universe", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Tracker_DefaultSnapshot_IsIdle()
    {
        ContinuumRefreshOperationTracker tracker = new();

        ContinuumRefreshOperationStatus snapshot = tracker.GetSnapshot();

        Assert.Equal("idle", snapshot.State);
        Assert.Equal("all-lists", snapshot.Scope);
        Assert.Null(snapshot.TargetSlug);
        Assert.Equal(0, snapshot.TotalListCount);
        Assert.Equal(0, snapshot.TotalUserOperationCount);
    }

    [Fact]
    public void Tracker_RejectsConcurrentManualRefresh()
    {
        ContinuumRefreshOperationTracker tracker = new();

        tracker.Start("chicago-universe");

        ContinuumRefreshConflictException exception = Assert.Throws<ContinuumRefreshConflictException>(() =>
            tracker.Start("marvel-cinematic-universe"));

        Assert.Contains("already running", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Tracker_TransitionsFromIdleToRunningToCompleted()
    {
        DateTimeOffset[] timeline =
        [
            new DateTimeOffset(2026, 6, 16, 4, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 6, 16, 4, 5, 0, TimeSpan.Zero)
        ];
        int index = 0;
        ContinuumRefreshOperationTracker tracker = new(() => timeline[Math.Min(index++, timeline.Length - 1)]);

        tracker.Start("chicago-universe");
        tracker.SetPlan(totalListCount: 1, totalUserOperationCount: 3);
        tracker.Advance("chicago-universe", processedListCount: 0, processedUserOperationCount: 2);

        ContinuumRefreshOperationStatus runningSnapshot = tracker.GetSnapshot();

        Assert.Equal("running", runningSnapshot.State);
        Assert.Equal("single-list", runningSnapshot.Scope);
        Assert.Equal("chicago-universe", runningSnapshot.TargetSlug);
        Assert.Equal("chicago-universe", runningSnapshot.CurrentListSlug);
        Assert.Equal(1, runningSnapshot.TotalListCount);
        Assert.Equal(3, runningSnapshot.TotalUserOperationCount);
        Assert.Equal(2, runningSnapshot.ProcessedUserOperationCount);

        tracker.Complete(new ContinuumRefreshResult
        {
            StartedAtUtc = timeline[0],
            CompletedAtUtc = timeline[1],
            ListsProcessed = 1
        });

        ContinuumRefreshOperationStatus completedSnapshot = tracker.GetSnapshot();

        Assert.Equal("completed", completedSnapshot.State);
        Assert.Null(completedSnapshot.CurrentListSlug);
        Assert.Equal(timeline[1], completedSnapshot.CompletedAtUtc);
        Assert.Equal(1, completedSnapshot.ProcessedListCount);
        Assert.Equal(3, completedSnapshot.ProcessedUserOperationCount);
    }

    [Fact]
    public void Tracker_FailureState_IsSurfacedForUi()
    {
        DateTimeOffset timestamp = new(2026, 6, 16, 5, 0, 0, TimeSpan.Zero);
        ContinuumRefreshOperationTracker tracker = new(() => timestamp);

        tracker.Start(null);
        tracker.SetPlan(totalListCount: 2, totalUserOperationCount: 8);
        tracker.Advance("chicago-universe", processedListCount: 1, processedUserOperationCount: 4);
        tracker.Fail(new InvalidOperationException("Resolver failed for chicago-universe."));

        ContinuumRefreshOperationStatus snapshot = tracker.GetSnapshot();

        Assert.Equal("failed", snapshot.State);
        Assert.Equal("Resolver failed for chicago-universe.", snapshot.Message);
        Assert.Equal(timestamp, snapshot.CompletedAtUtc);
        Assert.Null(snapshot.CurrentListSlug);
        Assert.Equal(1, snapshot.ProcessedListCount);
        Assert.Equal(4, snapshot.ProcessedUserOperationCount);
    }
}
