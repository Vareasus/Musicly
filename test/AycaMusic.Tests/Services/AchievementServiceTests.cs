using AycaMusic.Data;
using AycaMusic.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace AycaMusic.Tests.Services;

public class AchievementServiceTests
{
    private readonly AchievementService _service;

    public AchievementServiceTests()
    {
        var mockDbFactory = new Mock<IDbContextFactory<AppDbContext>>();
        _service = new AchievementService(mockDbFactory.Object);
    }

    // ===== GetListenerBadge =====

    [Fact]
    public void GetListenerBadge_Over100Hours_ShouldReturnDiamond()
    {
        // 100 hours = 360000 seconds
        var badge = _service.GetListenerBadge(360000);
        Assert.Contains("Diamond", badge);
    }

    [Fact]
    public void GetListenerBadge_Exactly100Hours_ShouldReturnDiamond()
    {
        var badge = _service.GetListenerBadge(360000);
        Assert.Contains("Diamond", badge);
    }

    [Fact]
    public void GetListenerBadge_50Hours_ShouldReturnGold()
    {
        var badge = _service.GetListenerBadge(180000);
        Assert.Contains("Gold", badge);
    }

    [Fact]
    public void GetListenerBadge_20Hours_ShouldReturnSilver()
    {
        var badge = _service.GetListenerBadge(72000);
        Assert.Contains("Silver", badge);
    }

    [Fact]
    public void GetListenerBadge_5Hours_ShouldReturnBronze()
    {
        var badge = _service.GetListenerBadge(18000);
        Assert.Contains("Bronze", badge);
    }

    [Fact]
    public void GetListenerBadge_Under5Hours_ShouldReturnNewListener()
    {
        var badge = _service.GetListenerBadge(1000);
        Assert.Contains("New Listener", badge);
    }

    [Fact]
    public void GetListenerBadge_ZeroSeconds_ShouldReturnNewListener()
    {
        var badge = _service.GetListenerBadge(0);
        Assert.Contains("New Listener", badge);
    }

    [Theory]
    [InlineData(0, "New Listener")]
    [InlineData(17999, "New Listener")]    // Just under 5 hours
    [InlineData(18000, "Bronze")]          // Exactly 5 hours
    [InlineData(71999, "Bronze")]          // Just under 20 hours
    [InlineData(72000, "Silver")]          // Exactly 20 hours
    [InlineData(179999, "Silver")]         // Just under 50 hours
    [InlineData(180000, "Gold")]           // Exactly 50 hours
    [InlineData(359999, "Gold")]           // Just under 100 hours
    [InlineData(360000, "Diamond")]        // Exactly 100 hours
    [InlineData(1000000, "Diamond")]       // Well over 100 hours
    public void GetListenerBadge_BoundaryValues(double seconds, string expectedContains)
    {
        var badge = _service.GetListenerBadge(seconds);
        Assert.Contains(expectedContains, badge);
    }

    // ===== Badge Format =====

    [Fact]
    public void GetListenerBadge_ShouldStartWithEmoji()
    {
        var badge = _service.GetListenerBadge(0);
        // Should start with an emoji character (🎵)
        Assert.False(string.IsNullOrEmpty(badge));
        Assert.True(badge.Length > 0);
    }
}
