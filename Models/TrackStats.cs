namespace Musicly.Models;

public class TrackStats
{
    public int TrackId { get; set; }
    public string Title { get; set; } = "";
    public string Artist { get; set; } = "";
    public int PlayCount { get; set; }
    public double TotalListeningSeconds { get; set; }
    public DateTime LastPlayed { get; set; }
    public string GradientColor { get; set; } = "";

    public string TotalListeningFormatted
    {
        get
        {
            var ts = TimeSpan.FromSeconds(TotalListeningSeconds);
            if (ts.TotalHours >= 1)
                return $"{(int)ts.TotalHours}h {ts.Minutes}m";
            return $"{ts.Minutes}m {ts.Seconds}s";
        }
    }
}
