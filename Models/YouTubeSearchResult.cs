namespace Musicly.Models;

public class YouTubeSearchResult
{
    public string VideoId { get; set; } = "";
    public string Title { get; set; } = "";
    public string ChannelName { get; set; } = "";
    public string ThumbnailUrl { get; set; } = "";
    public string Duration { get; set; } = "";
    public int DurationSeconds { get; set; }
}
