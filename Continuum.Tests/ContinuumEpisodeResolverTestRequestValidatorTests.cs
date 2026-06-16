using Continuum.Models;
using Continuum.Services;

namespace Continuum.Tests;

public sealed class ContinuumEpisodeResolverTestRequestValidatorTests
{
    [Fact]
    public void Validate_ReturnsErrorWhenNoMeaningfulHintIsProvided()
    {
        ContinuumEpisodeResolverTestRequest request = new()
        {
            SeasonNumber = 1,
            EpisodeNumber = 2
        };

        string? error = ContinuumEpisodeResolverTestRequestValidator.Validate(request);

        Assert.Equal("Provide at least one resolver hint such as a Jellyfin id, TVDb/TMDb id, or title.", error);
    }

    [Fact]
    public void Validate_ReturnsErrorForNegativeSeason()
    {
        ContinuumEpisodeResolverTestRequest request = new()
        {
            Title = "Pilot",
            SeasonNumber = -1
        };

        string? error = ContinuumEpisodeResolverTestRequestValidator.Validate(request);

        Assert.Equal("Season number must be zero or a positive integer when provided.", error);
    }

    [Fact]
    public void Validate_ReturnsErrorForNonPositiveEpisode()
    {
        ContinuumEpisodeResolverTestRequest request = new()
        {
            Title = "Pilot",
            EpisodeNumber = 0
        };

        string? error = ContinuumEpisodeResolverTestRequestValidator.Validate(request);

        Assert.Equal("Episode number must be a positive integer when provided.", error);
    }

    [Fact]
    public void Validate_ReturnsNullForValidRequest()
    {
        ContinuumEpisodeResolverTestRequest request = new()
        {
            TvdbSeriesId = "tvdb-series-1",
            SeasonNumber = 1,
            EpisodeNumber = 1
        };

        string? error = ContinuumEpisodeResolverTestRequestValidator.Validate(request);

        Assert.Null(error);
    }

    [Fact]
    public void Validate_AllowsSeasonZeroForSpecials()
    {
        ContinuumEpisodeResolverTestRequest request = new()
        {
            TvdbSeriesId = "tvdb-series-1",
            SeasonNumber = 0,
            EpisodeNumber = 1
        };

        string? error = ContinuumEpisodeResolverTestRequestValidator.Validate(request);

        Assert.Null(error);
    }
}
