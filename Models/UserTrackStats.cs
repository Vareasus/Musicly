namespace Musicly.Models;

public class UserTrackStats
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int TrackId { get; set; }
    public int PlayCount { get; set; }
    public double TotalListeningSeconds { get; set; }
    public DateTime LastPlayed { get; set; }
    public bool IsLiked { get; set; }
    public bool IsDisliked { get; set; }

    // Professional fields
    public int Rating { get; set; } // 0-5 star rating
    public DateTime? FirstPlayedAt { get; set; }

    public AppUser? User { get; set; }
    public DbTrack? Track { get; set; }
}
