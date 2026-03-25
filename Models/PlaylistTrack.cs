namespace AycaMusic.Models;

public class PlaylistTrack
{
    public int Id { get; set; }
    public int PlaylistId { get; set; }
    public int TrackId { get; set; }
    public int OrderIndex { get; set; }

    public Playlist? Playlist { get; set; }
    public DbTrack? Track { get; set; }
}
