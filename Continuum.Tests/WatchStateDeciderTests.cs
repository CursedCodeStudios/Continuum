using Continuum.Services;

namespace Continuum.Tests;

public class WatchStateDeciderTests
{
    [Fact]
    public void ShouldInclude_ReturnsFalseForPlayedItems()
    {
        WatchStateEvaluation evaluation = WatchStateEvaluation.FromPlayback(true, 100);

        Assert.False(WatchStateDecider.ShouldInclude(evaluation, includePartiallyWatched: true, includeUnwatched: true));
    }

    [Fact]
    public void ShouldInclude_HonorsPartiallyWatchedFlag()
    {
        WatchStateEvaluation evaluation = WatchStateEvaluation.FromPlayback(false, 100);

        Assert.True(WatchStateDecider.ShouldInclude(evaluation, includePartiallyWatched: true, includeUnwatched: false));
        Assert.False(WatchStateDecider.ShouldInclude(evaluation, includePartiallyWatched: false, includeUnwatched: true));
    }

    [Fact]
    public void ShouldInclude_HonorsUnwatchedFlag()
    {
        WatchStateEvaluation evaluation = WatchStateEvaluation.FromPlayback(false, 0);

        Assert.True(WatchStateDecider.ShouldInclude(evaluation, includePartiallyWatched: false, includeUnwatched: true));
        Assert.False(WatchStateDecider.ShouldInclude(evaluation, includePartiallyWatched: true, includeUnwatched: false));
    }

    [Fact]
    public void ShouldInclude_TreatsBothFlagsDisabledAsEnabled()
    {
        WatchStateEvaluation partial = WatchStateEvaluation.FromPlayback(false, 50);
        WatchStateEvaluation unwatched = WatchStateEvaluation.FromPlayback(false, 0);

        Assert.True(WatchStateDecider.ShouldInclude(partial, includePartiallyWatched: false, includeUnwatched: false));
        Assert.True(WatchStateDecider.ShouldInclude(unwatched, includePartiallyWatched: false, includeUnwatched: false));
    }
}
