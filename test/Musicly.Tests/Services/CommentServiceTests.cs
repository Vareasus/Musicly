using Musicly.Data;
using Musicly.Models;
using Musicly.Services;
using Microsoft.EntityFrameworkCore;

namespace Musicly.Tests.Services;

public class CommentServiceTests
{
    private AppDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    private (CommentService service, AppDbContext db) CreateService()
    {
        var db = CreateInMemoryDb();
        var notifService = new NotificationService(db);
        var service = new CommentService(db, notifService);
        return (service, db);
    }

    [Fact]
    public async Task AddCommentAsync_ShouldCreateComment()
    {
        var (service, db) = CreateService();
        var comment = await service.AddCommentAsync(1, 1, "user1", "Great song!", 5);

        Assert.Equal("Great song!", comment.Text);
        Assert.Equal(5, comment.Rating);
        Assert.Equal(1, comment.TrackId);
    }

    [Fact]
    public async Task AddCommentAsync_ShouldTrimText()
    {
        var (service, _) = CreateService();
        var comment = await service.AddCommentAsync(1, 1, "user1", "  Hello  ", 3);

        Assert.Equal("Hello", comment.Text);
    }

    [Fact]
    public async Task AddCommentAsync_ShouldClampRatingToMax5()
    {
        var (service, _) = CreateService();
        var comment = await service.AddCommentAsync(1, 1, "user1", "Test", 10);

        Assert.Equal(5, comment.Rating);
    }

    [Fact]
    public async Task AddCommentAsync_ShouldClampRatingToMin1()
    {
        var (service, _) = CreateService();
        var comment = await service.AddCommentAsync(1, 1, "user1", "Test", -5);

        Assert.Equal(1, comment.Rating);
    }

    [Fact]
    public async Task AddCommentAsync_ShouldClampRatingZeroTo1()
    {
        var (service, _) = CreateService();
        var comment = await service.AddCommentAsync(1, 1, "user1", "Test", 0);

        Assert.Equal(1, comment.Rating);
    }

    [Fact]
    public async Task AddCommentAsync_ShouldSetCreatedAtToUtcNow()
    {
        var (service, _) = CreateService();
        var before = DateTime.UtcNow;
        var comment = await service.AddCommentAsync(1, 1, "user1", "Test", 3);
        var after = DateTime.UtcNow;

        Assert.InRange(comment.CreatedAt, before, after);
    }

    [Fact]
    public async Task DeleteCommentAsync_ShouldRemoveComment()
    {
        var (service, db) = CreateService();
        var comment = await service.AddCommentAsync(1, 1, "user1", "To delete", 3);
        await service.DeleteCommentAsync(comment.Id);

        Assert.Empty(await db.TrackComments.ToListAsync());
    }

    [Fact]
    public async Task DeleteCommentAsync_NonExistentId_ShouldNotThrow()
    {
        var (service, _) = CreateService();
        await service.DeleteCommentAsync(9999);
    }

    [Fact]
    public async Task GetAverageRatingAsync_NoComments_ShouldReturnZero()
    {
        var (service, _) = CreateService();
        var (avg, count) = await service.GetAverageRatingAsync(1);

        Assert.Equal(0, avg);
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task GetAverageRatingAsync_WithComments_ShouldCalculateCorrectly()
    {
        var (service, _) = CreateService();
        await service.AddCommentAsync(1, 1, "u1", "Good", 4);
        await service.AddCommentAsync(1, 2, "u2", "Great", 5);
        await service.AddCommentAsync(1, 3, "u3", "OK", 3);

        var (avg, count) = await service.GetAverageRatingAsync(1);

        Assert.Equal(4.0, avg);
        Assert.Equal(3, count);
    }

    [Fact]
    public async Task GetAverageRatingAsync_SingleComment_ShouldReturnItsRating()
    {
        var (service, _) = CreateService();
        await service.AddCommentAsync(1, 1, "u1", "Solo", 5);

        var (avg, count) = await service.GetAverageRatingAsync(1);

        Assert.Equal(5.0, avg);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task ToggleLikeAsync_ShouldAddLike()
    {
        var (service, db) = CreateService();
        var comment = await service.AddCommentAsync(1, 1, "user1", "Nice", 4);

        var result = await service.ToggleLikeAsync(comment.Id, 2);

        Assert.True(result);
        Assert.Equal(1, await db.CommentLikes.CountAsync());
    }

    [Fact]
    public async Task ToggleLikeAsync_SecondTime_ShouldRemoveLike()
    {
        var (service, db) = CreateService();
        var comment = await service.AddCommentAsync(1, 1, "user1", "Nice", 4);

        await service.ToggleLikeAsync(comment.Id, 2);
        var result = await service.ToggleLikeAsync(comment.Id, 2);

        Assert.False(result);
        Assert.Equal(0, await db.CommentLikes.CountAsync());
    }

    [Fact]
    public async Task GetCommentsForTrackAsync_ShouldReturnCommentsForTrack()
    {
        var (service, _) = CreateService();
        await service.AddCommentAsync(1, 1, "u1", "Comment for track 1", 5);
        await service.AddCommentAsync(2, 1, "u1", "Comment for track 2", 4);
        await service.AddCommentAsync(1, 2, "u2", "Another for track 1", 3);

        var comments = await service.GetCommentsForTrackAsync(1, 1);

        Assert.Equal(2, comments.Count);
    }

    [Fact]
    public async Task GetUserCommentsAsync_ShouldReturnOnlyUserComments()
    {
        var (service, _) = CreateService();
        await service.AddCommentAsync(1, 1, "u1", "User 1 comment", 5);
        await service.AddCommentAsync(1, 2, "u2", "User 2 comment", 4);
        await service.AddCommentAsync(2, 1, "u1", "User 1 again", 3);

        var comments = await service.GetUserCommentsAsync(1);

        Assert.Equal(2, comments.Count);
        Assert.All(comments, c => Assert.Equal(1, c.UserId));
    }
}
