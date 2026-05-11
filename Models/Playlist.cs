namespace Musicly.Models;

public class Playlist
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public AppUser? User { get; set; }
    public List<PlaylistTrack> PlaylistTracks { get; set; } = new();
}
