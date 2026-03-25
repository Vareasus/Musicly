using AycaMusic.Data;
using AycaMusic.Models;
using Microsoft.EntityFrameworkCore;

namespace AycaMusic.Services;

public record Achievement(string Id, string Title, string Emoji, string Description, bool Unlocked);

public class AchievementService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public AchievementService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<Achievement>> GetAchievementsAsync(int userId)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var stats = await db.UserTrackStats.AsNoTracking()
            .Where(s => s.UserId == userId).ToListAsync();

        var totalPlays = stats.Sum(s => s.PlayCount);
        var totalSeconds = stats.Sum(s => s.TotalListeningSeconds);
        var totalHours = totalSeconds / 3600;
        var uniqueTracks = stats.Count(s => s.PlayCount > 0);
        var likedCount = stats.Count(s => s.IsLiked);

        // Check streak: get listening history
        var history = await db.Set<ListeningHistory>().AsNoTracking()
            .Where(h => h.UserId == userId)
            .OrderByDescending(h => h.StartedAt)
            .Take(100).ToListAsync();

        var streakDays = CalculateStreak(history);
        var uniqueGenres = await GetUniqueGenresPlayed(db, userId);

        return new List<Achievement>
        {
            new("first_play", "First Play", "🎵", "Play your first song", totalPlays >= 1),
            new("ten_plays", "Music Lover", "💜", "Play 10 songs", totalPlays >= 10),
            new("fifty_plays", "Superfan", "🔥", "Play 50 songs", totalPlays >= 50),
            new("hundred_plays", "Centurion", "💯", "Play 100 songs", totalPlays >= 100),
            new("one_hour", "Hour Glass", "⏳", "Listen for 1 hour total", totalHours >= 1),
            new("five_hours", "Marathon", "🏃", "Listen for 5 hours total", totalHours >= 5),
            new("twenty_hours", "Audiophile", "🎧", "Listen for 20 hours total", totalHours >= 20),
            new("three_streak", "On Fire", "🔥", "Listen 3 days in a row", streakDays >= 3),
            new("seven_streak", "Weekly Warrior", "⚔️", "Listen 7 days in a row", streakDays >= 7),
            new("five_likes", "Curator", "❤️", "Like 5 songs", likedCount >= 5),
            new("five_tracks", "Explorer", "🧭", "Listen to 5 different tracks", uniqueTracks >= 5),
            new("genre_explorer", "Genre Explorer", "🌍", "Listen to 3+ genres", uniqueGenres >= 3),
        };
    }

    public string GetListenerBadge(double totalSeconds)
    {
        var hours = totalSeconds / 3600;
        return hours switch
        {
            >= 100 => "💎 Diamond Listener",
            >= 50 => "🥇 Gold Listener",
            >= 20 => "🥈 Silver Listener",
            >= 5 => "🥉 Bronze Listener",
            _ => "🎵 New Listener"
        };
    }

    private int CalculateStreak(List<ListeningHistory> history)
    {
        if (history.Count == 0) return 0;
        var days = history.Select(h => h.StartedAt.Date).Distinct().OrderByDescending(d => d).ToList();
        if (days[0] < DateTime.UtcNow.Date.AddDays(-1)) return 0;

        int streak = 1;
        for (int i = 1; i < days.Count; i++)
        {
            if ((days[i - 1] - days[i]).TotalDays == 1) streak++;
            else break;
        }
        return streak;
    }

    private async Task<int> GetUniqueGenresPlayed(AppDbContext db, int userId)
    {
        var trackIds = await db.UserTrackStats.AsNoTracking()
            .Where(s => s.UserId == userId && s.PlayCount > 0)
            .Select(s => s.TrackId).ToListAsync();

        var genres = await db.Tracks.AsNoTracking()
            .Where(t => trackIds.Contains(t.Id))
            .Select(t => t.Genre).Distinct().CountAsync();

        return genres;
    }
}
