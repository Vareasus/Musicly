namespace Musicly.Models;

public class Track
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Artist { get; set; } = "Unknown Artist";
    public string Src { get; set; } = string.Empty;
    public string GradientColor { get; set; } = "linear-gradient(135deg, #1a1a2e, #16213e, #0f3460, #e94560)";
    public string IconSvg { get; set; } = string.Empty;
    public string CoverImage { get; set; } = string.Empty;
    public string Genre { get; set; } = "Ambient";
    public string Mood { get; set; } = "Chill";
    public List<LyricLine> Lyrics { get; set; } = new();
    public bool IsYouTube { get; set; }
    public string? YouTubeVideoId { get; set; }
}
