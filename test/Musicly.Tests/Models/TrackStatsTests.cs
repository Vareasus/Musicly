using Musicly.Models;

namespace Musicly.Tests.Models;

public class TrackStatsTests
{
    [Fact]
    public void TotalListeningFormatted_ZeroSeconds_ShouldReturnZeroMinutesZeroSeconds()
    {
        var stats = new TrackStats { TotalListeningSeconds = 0 };
        Assert.Equal("0m 0s", stats.TotalListeningFormatted);
    }

    [Fact]
    public void TotalListeningFormatted_30Seconds_ShouldReturnMinutesAndSeconds()
    {
        var stats = new TrackStats { TotalListeningSeconds = 30 };
        Assert.Equal("0m 30s", stats.TotalListeningFormatted);
    }

    [Fact]
    public void TotalListeningFormatted_90Seconds_ShouldReturnOneMinute30Seconds()
    {
        var stats = new TrackStats { TotalListeningSeconds = 90 };
        Assert.Equal("1m 30s", stats.TotalListeningFormatted);
    }

    [Fact]
    public void TotalListeningFormatted_3600Seconds_ShouldReturnOneHourFormat()
    {
        var stats = new TrackStats { TotalListeningSeconds = 3600 };
        Assert.Equal("1h 0m", stats.TotalListeningFormatted);
    }

    [Fact]
    public void TotalListeningFormatted_3661Seconds_ShouldReturnOneHourOneMinute()
    {
        var stats = new TrackStats { TotalListeningSeconds = 3661 };
        Assert.Equal("1h 1m", stats.TotalListeningFormatted);
    }

    [Fact]
    public void TotalListeningFormatted_7200Seconds_ShouldReturnTwoHours()
    {
        var stats = new TrackStats { TotalListeningSeconds = 7200 };
        Assert.Equal("2h 0m", stats.TotalListeningFormatted);
    }

    [Fact]
    public void TotalListeningFormatted_5999Seconds_JustUnderTwoHours()
    {
        // 5999s = 1h 39m 59s → "1h 39m"
        var stats = new TrackStats { TotalListeningSeconds = 5999 };
        Assert.Equal("1h 39m", stats.TotalListeningFormatted);
    }

    [Fact]
    public void TotalListeningFormatted_LargeValue_ShouldFormatCorrectly()
    {
        // 36000s = 10 hours
        var stats = new TrackStats { TotalListeningSeconds = 36000 };
        Assert.Equal("10h 0m", stats.TotalListeningFormatted);
    }
}
