using Musicly.Data;
using Musicly.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Musicly.Tests.Services;

public class ListeningStatsServiceTests
{
    private readonly ListeningStatsService _service;

    public ListeningStatsServiceTests()
    {
        var mockDbFactory = new Mock<IDbContextFactory<AppDbContext>>();
        _service = new ListeningStatsService(mockDbFactory.Object);
    }

    // ===== FormatSeconds =====

    [Fact]
    public void FormatSeconds_ZeroSeconds_ShouldReturnZeroMinutesZeroSeconds()
    {
        Assert.Equal("0m 0s", _service.FormatSeconds(0));
    }

    [Fact]
    public void FormatSeconds_30Seconds_ShouldFormatCorrectly()
    {
        Assert.Equal("0m 30s", _service.FormatSeconds(30));
    }

    [Fact]
    public void FormatSeconds_90Seconds_ShouldFormatAsMinutes()
    {
        Assert.Equal("1m 30s", _service.FormatSeconds(90));
    }

    [Fact]
    public void FormatSeconds_3600Seconds_ShouldFormatAsHours()
    {
        Assert.Equal("1h 0m 0s", _service.FormatSeconds(3600));
    }

    [Fact]
    public void FormatSeconds_3661Seconds_ShouldFormatCorrectly()
    {
        Assert.Equal("1h 1m 1s", _service.FormatSeconds(3661));
    }

    [Fact]
    public void FormatSeconds_7200Seconds_ShouldFormatAsTwoHours()
    {
        Assert.Equal("2h 0m 0s", _service.FormatSeconds(7200));
    }

    [Theory]
    [InlineData(59, "0m 59s")]
    [InlineData(60, "1m 0s")]
    [InlineData(3599, "59m 59s")]
    [InlineData(3600, "1h 0m 0s")]
    public void FormatSeconds_BoundaryValues(double seconds, string expected)
    {
        Assert.Equal(expected, _service.FormatSeconds(seconds));
    }

    // ===== SetCurrentUser =====

    [Fact]
    public void SetCurrentUser_ShouldNotThrow()
    {
        var ex = Record.Exception(() => _service.SetCurrentUser(1));
        Assert.Null(ex);
    }

    // ===== RecordTimeSlice with no user =====

    [Fact]
    public void RecordTimeSlice_NoUserSet_ShouldNotThrow()
    {
        var ex = Record.Exception(() => _service.RecordTimeSlice());
        Assert.Null(ex);
    }
}
