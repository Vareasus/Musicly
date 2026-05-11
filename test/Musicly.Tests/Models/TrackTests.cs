using Musicly.Models;

namespace Musicly.Tests.Models;

public class TrackTests
{
    // ===== Track Default Values =====

    [Fact]
    public void Track_DefaultArtist_ShouldBeUnknownArtist()
    {
        var track = new Track();
        Assert.Equal("Unknown Artist", track.Artist);
    }

    [Fact]
    public void Track_DefaultGenre_ShouldBeAmbient()
    {
        var track = new Track();
        Assert.Equal("Ambient", track.Genre);
    }

    [Fact]
    public void Track_DefaultMood_ShouldBeChill()
    {
        var track = new Track();
        Assert.Equal("Chill", track.Mood);
    }

    [Fact]
    public void Track_DefaultTitle_ShouldBeEmpty()
    {
        var track = new Track();
        Assert.Equal(string.Empty, track.Title);
    }

    [Fact]
    public void Track_DefaultLyrics_ShouldBeEmptyList()
    {
        var track = new Track();
        Assert.NotNull(track.Lyrics);
        Assert.Empty(track.Lyrics);
    }

    [Fact]
    public void Track_DefaultGradientColor_ShouldContainLinearGradient()
    {
        var track = new Track();
        Assert.StartsWith("linear-gradient", track.GradientColor);
    }

    // ===== DbTrack Default Values =====

    [Fact]
    public void DbTrack_DefaultArtist_ShouldBeUnknownArtist()
    {
        var track = new DbTrack();
        Assert.Equal("Unknown Artist", track.Artist);
    }

    [Fact]
    public void DbTrack_DefaultGenre_ShouldBeAmbient()
    {
        var track = new DbTrack();
        Assert.Equal("Ambient", track.Genre);
    }

    [Fact]
    public void DbTrack_DefaultMood_ShouldBeChill()
    {
        var track = new DbTrack();
        Assert.Equal("Chill", track.Mood);
    }

    [Fact]
    public void DbTrack_CreatedAt_ShouldBeCloseToUtcNow()
    {
        var before = DateTime.UtcNow;
        var track = new DbTrack();
        var after = DateTime.UtcNow;

        Assert.InRange(track.CreatedAt, before, after);
    }

    [Fact]
    public void DbTrack_AddedByUserId_ShouldBeNullByDefault()
    {
        var track = new DbTrack();
        Assert.Null(track.AddedByUserId);
    }

    // ===== AppUser Default Values =====

    [Fact]
    public void AppUser_DefaultRole_ShouldBeUser()
    {
        var user = new AppUser();
        Assert.Equal("User", user.Role);
    }

    [Fact]
    public void AppUser_DefaultIsActive_ShouldBeTrue()
    {
        var user = new AppUser();
        Assert.True(user.IsActive);
    }

    [Fact]
    public void AppUser_DefaultProfileImageUrl_ShouldBeNull()
    {
        var user = new AppUser();
        Assert.Null(user.ProfileImageUrl);
    }

    [Fact]
    public void AppUser_DefaultLastLoginAt_ShouldBeNull()
    {
        var user = new AppUser();
        Assert.Null(user.LastLoginAt);
    }

    // ===== Notification Default Values =====

    [Fact]
    public void Notification_DefaultIsRead_ShouldBeFalse()
    {
        var notification = new Notification();
        Assert.False(notification.IsRead);
    }

    [Fact]
    public void Notification_DefaultMessage_ShouldBeEmpty()
    {
        var notification = new Notification();
        Assert.Equal("", notification.Message);
    }

    // ===== Playlist Default Values =====

    [Fact]
    public void Playlist_DefaultPlaylistTracks_ShouldBeEmptyList()
    {
        var playlist = new Playlist();
        Assert.NotNull(playlist.PlaylistTracks);
        Assert.Empty(playlist.PlaylistTracks);
    }

    [Fact]
    public void Playlist_DefaultDescription_ShouldBeNull()
    {
        var playlist = new Playlist();
        Assert.Null(playlist.Description);
    }

    // ===== LyricLine =====

    [Fact]
    public void LyricLine_DefaultText_ShouldBeEmpty()
    {
        var line = new LyricLine();
        Assert.Equal(string.Empty, line.Text);
    }

    [Fact]
    public void LyricLine_DefaultTime_ShouldBeZero()
    {
        var line = new LyricLine();
        Assert.Equal(0, line.Time);
    }

    // ===== CommentLike =====

    [Fact]
    public void CommentLike_CreatedAt_ShouldBeCloseToUtcNow()
    {
        var before = DateTime.UtcNow;
        var like = new CommentLike();
        var after = DateTime.UtcNow;

        Assert.InRange(like.CreatedAt, before, after);
    }

    // ===== TrackComment =====

    [Fact]
    public void TrackComment_DefaultLikes_ShouldBeEmptyList()
    {
        var comment = new TrackComment();
        Assert.NotNull(comment.Likes);
        Assert.Empty(comment.Likes);
    }

    [Fact]
    public void TrackComment_DefaultRating_ShouldBeZero()
    {
        var comment = new TrackComment();
        Assert.Equal(0, comment.Rating);
    }

    // ===== UserTrackStats =====

    [Fact]
    public void UserTrackStats_DefaultIsLiked_ShouldBeFalse()
    {
        var stats = new UserTrackStats();
        Assert.False(stats.IsLiked);
    }

    [Fact]
    public void UserTrackStats_DefaultIsDisliked_ShouldBeFalse()
    {
        var stats = new UserTrackStats();
        Assert.False(stats.IsDisliked);
    }

    [Fact]
    public void UserTrackStats_DefaultRating_ShouldBeZero()
    {
        var stats = new UserTrackStats();
        Assert.Equal(0, stats.Rating);
    }

    [Fact]
    public void UserTrackStats_DefaultFirstPlayedAt_ShouldBeNull()
    {
        var stats = new UserTrackStats();
        Assert.Null(stats.FirstPlayedAt);
    }
}
