namespace AycaMusic.Models;

/// <summary>
/// Serializable container for all listening stats – saved/loaded as JSON.
/// </summary>
public class StatsData
{
    public Dictionary<int, TrackStats> Stats { get; set; } = new();
    public HashSet<string> ListeningDays { get; set; } = new();
    public Dictionary<int, int> HourHistogram { get; set; } = new();
    public List<double> SessionDurations { get; set; } = new();
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public HashSet<int> LikedTrackIds { get; set; } = new();
    public HashSet<int> DislikedTrackIds { get; set; } = new();
}
