namespace AycaMusic.Models;

/// <summary>
/// Database-stored track. The in-memory Track model still used for runtime playback.
/// </summary>
public class DbTrack
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Artist { get; set; } = "Unknown Artist";
    public string FilePath { get; set; } = ""; // e.g. "music/song.mp3"
    public string Genre { get; set; } = "Ambient";
    public string Mood { get; set; } = "Chill";
    public string GradientColor { get; set; } = "linear-gradient(135deg, #1a1a2e, #16213e)";
    public string IconSvg { get; set; } = "";
    public double DurationSeconds { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? AddedByUserId { get; set; }

    public AppUser? AddedBy { get; set; }
}
