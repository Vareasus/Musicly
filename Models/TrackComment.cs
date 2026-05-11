namespace Musicly.Models;

public class TrackComment
{
    public int Id { get; set; }
    public int TrackId { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = "";
    public string Text { get; set; } = "";
    public int Rating { get; set; } // 1-5 stars
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public DbTrack? Track { get; set; }
    public AppUser? User { get; set; }
    public List<CommentLike> Likes { get; set; } = new();
}
