using Musicly.Data;
using Musicly.Models;
using Microsoft.EntityFrameworkCore;

namespace Musicly.Services;

public class CommentService
{
    private readonly AppDbContext _db;
    private readonly NotificationService _notifService;

    private readonly MusicPlayerService _player;

    public CommentService(AppDbContext db, NotificationService notifService, MusicPlayerService player)
    {
        _db = db;
        _notifService = notifService;
        _player = player;
    }

    /// <summary>Get all comments for a track, ordered by newest first, with like counts</summary>
    public async Task<List<CommentWithLikes>> GetCommentsForTrackAsync(int trackId, int currentUserId)
    {
        if (trackId < 0)
        {
            var track = _player.Tracks.FirstOrDefault(t => t.Id == trackId);
            if (track != null && track.IsYouTube)
            {
                var dbTrack = await _db.Tracks.AsNoTracking().FirstOrDefaultAsync(t => t.FilePath.StartsWith($"youtube:{track.YouTubeVideoId}|") || t.FilePath == $"youtube:{track.YouTubeVideoId}");
                if (dbTrack != null)
                {
                    trackId = dbTrack.Id;
                    track.Id = dbTrack.Id;
                    _player.NotifyStateChanged();
                }
                else
                {
                    return new List<CommentWithLikes>();
                }
            }
            else
            {
                return new List<CommentWithLikes>();
            }
        }

        var comments = await _db.TrackComments
            .Where(c => c.TrackId == trackId)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new CommentWithLikes
            {
                Id = c.Id,
                TrackId = c.TrackId,
                UserId = c.UserId,
                Username = c.Username,
                Text = c.Text,
                Rating = c.Rating,
                CreatedAt = c.CreatedAt,
                LikeCount = c.Likes.Count,
                IsLikedByCurrentUser = c.Likes.Any(l => l.UserId == currentUserId)
            })
            .ToListAsync();

        return comments;
    }

    /// <summary>Add a new comment</summary>
    public async Task<TrackComment> AddCommentAsync(int trackId, int userId, string username, string text, int rating)
    {
        rating = Math.Clamp(rating, 1, 5);

        if (trackId < 0)
        {
            var track = _player.Tracks.FirstOrDefault(t => t.Id == trackId);
            if (track != null)
            {
                trackId = await _player.EnsureYouTubeTrackSavedAsync(track);
            }
            else
            {
                throw new InvalidOperationException("Track not found");
            }
        }

        var comment = new TrackComment
        {
            TrackId = trackId,
            UserId = userId,
            Username = username,
            Text = text.Trim(),
            Rating = rating,
            CreatedAt = DateTime.UtcNow
        };

        _db.TrackComments.Add(comment);
        await _db.SaveChangesAsync();
        return comment;
    }

    /// <summary>Toggle like on a comment and notify the author</summary>
    public async Task<bool> ToggleLikeAsync(int commentId, int userId, string likerUsername = "")
    {
        var existing = await _db.CommentLikes
            .FirstOrDefaultAsync(l => l.CommentId == commentId && l.UserId == userId);

        if (existing != null)
        {
            _db.CommentLikes.Remove(existing);
            await _db.SaveChangesAsync();
            return false; // unliked
        }

        _db.CommentLikes.Add(new CommentLike
        {
            CommentId = commentId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        // Send notification to comment author
        var comment = await _db.TrackComments
            .Include(c => c.Track)
            .FirstOrDefaultAsync(c => c.Id == commentId);
        if (comment != null && comment.UserId != userId)
        {
            var trackName = comment.Track?.Title ?? "bir şarkı";
            var msg = $"💜 {likerUsername} senin \"{trackName}\" yorumunu beğendi!";
            await _notifService.NotifyUserAsync(comment.UserId, msg);
        }

        return true; // liked
    }

    /// <summary>Delete a comment (admin)</summary>
    public async Task DeleteCommentAsync(int commentId)
    {
        var comment = await _db.TrackComments.FindAsync(commentId);
        if (comment != null)
        {
            _db.TrackComments.Remove(comment);
            await _db.SaveChangesAsync();
        }
    }

    /// <summary>Get all comments for admin moderation</summary>
    public async Task<List<CommentWithLikes>> GetAllCommentsAsync()
    {
        return await _db.TrackComments
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new CommentWithLikes
            {
                Id = c.Id,
                TrackId = c.TrackId,
                UserId = c.UserId,
                Username = c.Username,
                Text = c.Text,
                Rating = c.Rating,
                CreatedAt = c.CreatedAt,
                LikeCount = c.Likes.Count,
                TrackTitle = c.Track != null ? c.Track.Title : "Unknown"
            })
            .ToListAsync();
    }

    /// <summary>Get all comments by a specific user</summary>
    public async Task<List<CommentWithLikes>> GetUserCommentsAsync(int userId)
    {
        return await _db.TrackComments
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new CommentWithLikes
            {
                Id = c.Id,
                TrackId = c.TrackId,
                UserId = c.UserId,
                Username = c.Username,
                Text = c.Text,
                Rating = c.Rating,
                CreatedAt = c.CreatedAt,
                LikeCount = c.Likes.Count,
                TrackTitle = c.Track != null ? c.Track.Title : "Unknown"
            })
            .ToListAsync();
    }

    /// <summary>Get liked track IDs for a specific user (for profile)</summary>
    public async Task<List<int>> GetLikedTrackIdsForUserAsync(int userId)
    {
        return await _db.Set<UserTrackStats>()
            .Where(s => s.UserId == userId && s.IsLiked)
            .Select(s => s.TrackId)
            .ToListAsync();
    }

    /// <summary>Get average rating for a track</summary>
    public async Task<(double Average, int Count)> GetAverageRatingAsync(int trackId)
    {
        if (trackId < 0)
        {
            var track = _player.Tracks.FirstOrDefault(t => t.Id == trackId);
            if (track != null && track.IsYouTube)
            {
                var dbTrack = await _db.Tracks.AsNoTracking().FirstOrDefaultAsync(t => t.FilePath.StartsWith($"youtube:{track.YouTubeVideoId}|") || t.FilePath == $"youtube:{track.YouTubeVideoId}");
                if (dbTrack != null)
                {
                    trackId = dbTrack.Id;
                }
                else
                {
                    return (0, 0);
                }
            }
            else
            {
                return (0, 0);
            }
        }

        var ratings = await _db.TrackComments
            .Where(c => c.TrackId == trackId)
            .Select(c => c.Rating)
            .ToListAsync();

        if (ratings.Count == 0) return (0, 0);
        return (ratings.Average(), ratings.Count);
    }
}

/// <summary>DTO for comments with computed like info</summary>
public class CommentWithLikes
{
    public int Id { get; set; }
    public int TrackId { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = "";
    public string Text { get; set; } = "";
    public int Rating { get; set; }
    public DateTime CreatedAt { get; set; }
    public int LikeCount { get; set; }
    public bool IsLikedByCurrentUser { get; set; }
    public string TrackTitle { get; set; } = "";
}
