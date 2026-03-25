using Musicly.Models;

namespace Musicly.Tests.Models;

public class SongRequestTests
{
    [Fact]
    public void SongRequest_DefaultStatus_ShouldBePending()
    {
        var request = new SongRequest();
        Assert.Equal(RequestStatus.Pending, request.Status);
    }

    [Fact]
    public void SongRequest_DefaultCategory_ShouldBeSong()
    {
        var request = new SongRequest();
        Assert.Equal(RequestCategory.Song, request.Category);
    }

    [Fact]
    public void SongRequest_SongTitle_ShouldMapToTitle()
    {
        var request = new SongRequest { Title = "Test Song" };
        Assert.Equal("Test Song", request.SongTitle);
    }

    [Fact]
    public void SongRequest_ArtistName_ShouldReturnEmpty()
    {
        var request = new SongRequest();
        Assert.Equal("", request.ArtistName);
    }

    [Fact]
    public void SongRequest_Message_ShouldMapToDescription()
    {
        var request = new SongRequest { Description = "Please add this" };
        Assert.Equal("Please add this", request.Message);
    }

    [Fact]
    public void SongRequest_Message_ShouldBeNullWhenDescriptionIsNull()
    {
        var request = new SongRequest { Description = null };
        Assert.Null(request.Message);
    }

    [Fact]
    public void SongRequest_DefaultAdminResponse_ShouldBeNull()
    {
        var request = new SongRequest();
        Assert.Null(request.AdminResponse);
    }

    [Fact]
    public void SongRequest_DefaultRespondedAt_ShouldBeNull()
    {
        var request = new SongRequest();
        Assert.Null(request.RespondedAt);
    }

    // ===== Enum Tests =====

    [Theory]
    [InlineData(RequestStatus.Pending, 0)]
    [InlineData(RequestStatus.Approved, 1)]
    [InlineData(RequestStatus.Rejected, 2)]
    public void RequestStatus_ShouldHaveCorrectValues(RequestStatus status, int expected)
    {
        Assert.Equal(expected, (int)status);
    }

    [Theory]
    [InlineData(RequestCategory.Song, 0)]
    [InlineData(RequestCategory.Feature, 1)]
    [InlineData(RequestCategory.Bug, 2)]
    [InlineData(RequestCategory.Other, 3)]
    public void RequestCategory_ShouldHaveCorrectValues(RequestCategory category, int expected)
    {
        Assert.Equal(expected, (int)category);
    }
}
