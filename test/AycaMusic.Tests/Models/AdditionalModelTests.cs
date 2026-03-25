using AycaMusic.Models;
using AycaMusic.Services;

namespace AycaMusic.Tests.Models;

public class ListeningHistoryTests
{
    [Fact]
    public void ListeningHistory_DefaultStartedAt_ShouldBeCloseToUtcNow()
    {
        var before = DateTime.UtcNow;
        var history = new ListeningHistory();
        var after = DateTime.UtcNow;

        Assert.InRange(history.StartedAt, before, after);
    }

    [Fact]
    public void ListeningHistory_DefaultDurationSeconds_ShouldBeZero()
    {
        var history = new ListeningHistory();
        Assert.Equal(0, history.DurationSeconds);
    }

    [Fact]
    public void ListeningHistory_DefaultNavigationProperties_ShouldBeNull()
    {
        var history = new ListeningHistory();
        Assert.Null(history.User);
        Assert.Null(history.Track);
    }

    [Fact]
    public void ListeningHistory_ShouldStoreDuration()
    {
        var history = new ListeningHistory { DurationSeconds = 245.5 };
        Assert.Equal(245.5, history.DurationSeconds);
    }
}

public class PlaylistTrackTests
{
    [Fact]
    public void PlaylistTrack_DefaultOrderIndex_ShouldBeZero()
    {
        var pt = new PlaylistTrack();
        Assert.Equal(0, pt.OrderIndex);
    }

    [Fact]
    public void PlaylistTrack_DefaultNavigationProperties_ShouldBeNull()
    {
        var pt = new PlaylistTrack();
        Assert.Null(pt.Playlist);
        Assert.Null(pt.Track);
    }

    [Fact]
    public void PlaylistTrack_ShouldStorePlaylistAndTrackIds()
    {
        var pt = new PlaylistTrack { PlaylistId = 5, TrackId = 10, OrderIndex = 3 };
        Assert.Equal(5, pt.PlaylistId);
        Assert.Equal(10, pt.TrackId);
        Assert.Equal(3, pt.OrderIndex);
    }
}

public class StatsDataTests
{
    [Fact]
    public void StatsData_DefaultStats_ShouldBeEmptyDictionary()
    {
        var data = new StatsData();
        Assert.NotNull(data.Stats);
        Assert.Empty(data.Stats);
    }

    [Fact]
    public void StatsData_DefaultListeningDays_ShouldBeEmptyHashSet()
    {
        var data = new StatsData();
        Assert.NotNull(data.ListeningDays);
        Assert.Empty(data.ListeningDays);
    }

    [Fact]
    public void StatsData_DefaultHourHistogram_ShouldBeEmptyDictionary()
    {
        var data = new StatsData();
        Assert.NotNull(data.HourHistogram);
        Assert.Empty(data.HourHistogram);
    }

    [Fact]
    public void StatsData_DefaultSessionDurations_ShouldBeEmptyList()
    {
        var data = new StatsData();
        Assert.NotNull(data.SessionDurations);
        Assert.Empty(data.SessionDurations);
    }

    [Fact]
    public void StatsData_DefaultCurrentStreak_ShouldBeZero()
    {
        var data = new StatsData();
        Assert.Equal(0, data.CurrentStreak);
    }

    [Fact]
    public void StatsData_DefaultLongestStreak_ShouldBeZero()
    {
        var data = new StatsData();
        Assert.Equal(0, data.LongestStreak);
    }

    [Fact]
    public void StatsData_DefaultLikedTrackIds_ShouldBeEmptyHashSet()
    {
        var data = new StatsData();
        Assert.NotNull(data.LikedTrackIds);
        Assert.Empty(data.LikedTrackIds);
    }

    [Fact]
    public void StatsData_DefaultDislikedTrackIds_ShouldBeEmptyHashSet()
    {
        var data = new StatsData();
        Assert.NotNull(data.DislikedTrackIds);
        Assert.Empty(data.DislikedTrackIds);
    }

    [Fact]
    public void StatsData_ShouldAllowAddingListeningDays()
    {
        var data = new StatsData();
        data.ListeningDays.Add("2026-03-25");
        data.ListeningDays.Add("2026-03-25"); // duplicate
        Assert.Single(data.ListeningDays);
    }

    [Fact]
    public void StatsData_ShouldTrackStreaks()
    {
        var data = new StatsData { CurrentStreak = 5, LongestStreak = 10 };
        Assert.Equal(5, data.CurrentStreak);
        Assert.Equal(10, data.LongestStreak);
    }

    [Fact]
    public void StatsData_HourHistogram_ShouldStoreValues()
    {
        var data = new StatsData();
        data.HourHistogram[14] = 100;
        data.HourHistogram[22] = 50;
        Assert.Equal(2, data.HourHistogram.Count);
        Assert.Equal(100, data.HourHistogram[14]);
    }

    [Fact]
    public void StatsData_LikedAndDisliked_ShouldBeIndependent()
    {
        var data = new StatsData();
        data.LikedTrackIds.Add(1);
        data.LikedTrackIds.Add(2);
        data.DislikedTrackIds.Add(3);
        Assert.Equal(2, data.LikedTrackIds.Count);
        Assert.Single(data.DislikedTrackIds);
        Assert.DoesNotContain(3, data.LikedTrackIds);
    }
}

public class CommentWithLikesTests
{
    [Fact]
    public void CommentWithLikes_DefaultUsername_ShouldBeEmpty()
    {
        var c = new CommentWithLikes();
        Assert.Equal("", c.Username);
    }

    [Fact]
    public void CommentWithLikes_DefaultText_ShouldBeEmpty()
    {
        var c = new CommentWithLikes();
        Assert.Equal("", c.Text);
    }

    [Fact]
    public void CommentWithLikes_DefaultTrackTitle_ShouldBeEmpty()
    {
        var c = new CommentWithLikes();
        Assert.Equal("", c.TrackTitle);
    }

    [Fact]
    public void CommentWithLikes_DefaultLikeCount_ShouldBeZero()
    {
        var c = new CommentWithLikes();
        Assert.Equal(0, c.LikeCount);
    }

    [Fact]
    public void CommentWithLikes_DefaultIsLikedByCurrentUser_ShouldBeFalse()
    {
        var c = new CommentWithLikes();
        Assert.False(c.IsLikedByCurrentUser);
    }

    [Fact]
    public void CommentWithLikes_DefaultRating_ShouldBeZero()
    {
        var c = new CommentWithLikes();
        Assert.Equal(0, c.Rating);
    }
}

public class TrackStatsAdditionalTests
{
    [Fact]
    public void TrackStats_DefaultPlayCount_ShouldBeZero()
    {
        var stats = new TrackStats();
        Assert.Equal(0, stats.PlayCount);
    }

    [Fact]
    public void TrackStats_DefaultTotalListeningSeconds_ShouldBeZero()
    {
        var stats = new TrackStats();
        Assert.Equal(0, stats.TotalListeningSeconds);
    }

    [Fact]
    public void TrackStats_DefaultLastPlayed_ShouldBeDefault()
    {
        var stats = new TrackStats();
        Assert.Equal(default(DateTime), stats.LastPlayed);
    }

    [Theory]
    [InlineData(1, "0m 1s")]
    [InlineData(59, "0m 59s")]
    [InlineData(61, "1m 1s")]
    [InlineData(119, "1m 59s")]
    [InlineData(120, "2m 0s")]
    public void TrackStats_TotalListeningFormatted_AdditionalEdgeCases(double seconds, string expected)
    {
        var stats = new TrackStats { TotalListeningSeconds = seconds };
        Assert.Equal(expected, stats.TotalListeningFormatted);
    }
}
