namespace AycaMusic.Models;

public class ListeningHistory
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int TrackId { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public double DurationSeconds { get; set; }

    public AppUser? User { get; set; }
    public DbTrack? Track { get; set; }
}
