using Musicly.Data;
using Musicly.Models;
using Microsoft.EntityFrameworkCore;

namespace Musicly.Services;

public class ListeningStatsService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private int _currentUserId;
    private int _currentTrackId = -1;
    private DateTime _lastUpdateTime = DateTime.UtcNow;
    private double _pendingSeconds;

    public event Action? OnStatsChanged;

    public ListeningStatsService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public void SetCurrentUser(int userId)
    {
        _currentUserId = userId;
    }

    public async Task TrackStartedAsync(Track track)
    {
        if (_currentUserId <= 0) return;
        try
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            var stat = await GetOrCreateStatAsync(db, track.Id);
            stat.PlayCount++;
            stat.LastPlayed = DateTime.UtcNow;
            stat.FirstPlayedAt ??= DateTime.UtcNow;
            _currentTrackId = track.Id;
            _lastUpdateTime = DateTime.UtcNow;

            // Record listening history
            db.Set<ListeningHistory>().Add(new ListeningHistory
            {
                UserId = _currentUserId,
                TrackId = track.Id,
                StartedAt = DateTime.UtcNow
            });

            await db.SaveChangesAsync();
            OnStatsChanged?.Invoke();
        }
        catch { }
    }

    public void RecordTimeSlice()
    {
        if (_currentUserId <= 0 || _currentTrackId < 0) return;

        var now = DateTime.UtcNow;
        var delta = (now - _lastUpdateTime).TotalSeconds;
        _lastUpdateTime = now;

        if (delta > 0 && delta < 2)
            _pendingSeconds += delta;
    }

    public async Task FlushAsync()
    {
        if (_currentUserId <= 0 || _currentTrackId < 0 || _pendingSeconds <= 0) return;
        try
        {
            var seconds = _pendingSeconds;
            _pendingSeconds = 0;
            using var db = await _dbFactory.CreateDbContextAsync();
            var stat = await db.UserTrackStats.FirstOrDefaultAsync(
                s => s.UserId == _currentUserId && s.TrackId == _currentTrackId);
            if (stat != null)
            {
                stat.TotalListeningSeconds += seconds;
                await db.SaveChangesAsync();
            }
        }
        catch { }
    }

    // === Stats Getters ===

    public async Task<List<UserTrackStats>> GetAllStatsAsync()
    {
        if (_currentUserId <= 0) return new();
        using var db = await _dbFactory.CreateDbContextAsync();
        return await db.UserTrackStats
            .AsNoTracking()
            .Where(s => s.UserId == _currentUserId && s.PlayCount > 0)
            .OrderByDescending(s => s.PlayCount)
            .ToListAsync();
    }

    public async Task<int> GetTotalPlaysAsync()
    {
        if (_currentUserId <= 0) return 0;
        using var db = await _dbFactory.CreateDbContextAsync();
        return await db.UserTrackStats.Where(s => s.UserId == _currentUserId).SumAsync(s => s.PlayCount);
    }

    public async Task<double> GetTotalListeningSecondsAsync()
    {
        if (_currentUserId <= 0) return 0;
        using var db = await _dbFactory.CreateDbContextAsync();
        return await db.UserTrackStats.Where(s => s.UserId == _currentUserId).SumAsync(s => s.TotalListeningSeconds);
    }

    public string FormatSeconds(double seconds)
    {
        var ts = TimeSpan.FromSeconds(seconds);
        if (ts.TotalHours >= 1)
            return $"{(int)ts.TotalHours}h {ts.Minutes}m {ts.Seconds}s";
        return $"{ts.Minutes}m {ts.Seconds}s";
    }

    public async Task<UserTrackStats?> GetMostPlayedAsync()
    {
        if (_currentUserId <= 0) return null;
        using var db = await _dbFactory.CreateDbContextAsync();
        return await db.UserTrackStats
            .AsNoTracking()
            .Where(s => s.UserId == _currentUserId && s.PlayCount > 0)
            .OrderByDescending(s => s.PlayCount).FirstOrDefaultAsync();
    }

    public async Task<List<UserTrackStats>> GetRecentlyPlayedAsync(int count = 5)
    {
        if (_currentUserId <= 0) return new();
        using var db = await _dbFactory.CreateDbContextAsync();
        return await db.UserTrackStats
            .AsNoTracking()
            .Where(s => s.UserId == _currentUserId && s.LastPlayed != default)
            .OrderByDescending(s => s.LastPlayed)
            .Take(count)
            .ToListAsync();
    }

    // === Like/Dislike ===

    public async Task<bool> IsLikedAsync(int trackId)
    {
        if (_currentUserId <= 0) return false;
        using var db = await _dbFactory.CreateDbContextAsync();
        var stat = await db.UserTrackStats.AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == _currentUserId && s.TrackId == trackId);
        return stat?.IsLiked ?? false;
    }

    public async Task<bool> IsDislikedAsync(int trackId)
    {
        if (_currentUserId <= 0) return false;
        using var db = await _dbFactory.CreateDbContextAsync();
        var stat = await db.UserTrackStats.AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == _currentUserId && s.TrackId == trackId);
        return stat?.IsDisliked ?? false;
    }

    public async Task ToggleLikeAsync(int trackId)
    {
        if (_currentUserId <= 0) return;
        try
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            var stat = await GetOrCreateStatAsync(db, trackId);
            stat.IsLiked = !stat.IsLiked;
            if (stat.IsLiked) stat.IsDisliked = false;
            await db.SaveChangesAsync();
            OnStatsChanged?.Invoke();
        }
        catch { }
    }

    public async Task ToggleDislikeAsync(int trackId)
    {
        if (_currentUserId <= 0) return;
        try
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            var stat = await GetOrCreateStatAsync(db, trackId);
            stat.IsDisliked = !stat.IsDisliked;
            if (stat.IsDisliked) stat.IsLiked = false;
            await db.SaveChangesAsync();
            OnStatsChanged?.Invoke();
        }
        catch { }
    }

    public async Task<List<int>> GetLikedTrackIdsAsync()
    {
        if (_currentUserId <= 0) return new();
        using var db = await _dbFactory.CreateDbContextAsync();
        return await db.UserTrackStats
            .AsNoTracking()
            .Where(s => s.UserId == _currentUserId && s.IsLiked)
            .Select(s => s.TrackId)
            .ToListAsync();
    }

    public async Task<HashSet<int>> GetDislikedTrackIdsAsync()
    {
        if (_currentUserId <= 0) return new();
        using var db = await _dbFactory.CreateDbContextAsync();
        var ids = await db.UserTrackStats
            .AsNoTracking()
            .Where(s => s.UserId == _currentUserId && s.IsDisliked)
            .Select(s => s.TrackId)
            .ToListAsync();
        return ids.ToHashSet();
    }

    public async Task<int> GetLikedCountAsync()
    {
        if (_currentUserId <= 0) return 0;
        using var db = await _dbFactory.CreateDbContextAsync();
        return await db.UserTrackStats.CountAsync(s => s.UserId == _currentUserId && s.IsLiked);
    }

    // === Admin Helpers ===

    public async Task<List<UserTrackStats>> GetAllStatsForUserAsync(int userId)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        return await db.UserTrackStats
            .AsNoTracking()
            .Where(s => s.UserId == userId && s.PlayCount > 0)
            .OrderByDescending(s => s.PlayCount)
            .ToListAsync();
    }

    public async Task<List<UserTrackStats>> GetTopTracksForUserAsync(int userId, int count = 5)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        return await db.UserTrackStats
            .AsNoTracking()
            .Where(s => s.UserId == userId && s.PlayCount > 0)
            .OrderByDescending(s => s.TotalListeningSeconds)
            .Take(count)
            .ToListAsync();
    }

    public async Task<int> GetTotalPlaysForUserAsync(int userId)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        return await db.UserTrackStats.Where(s => s.UserId == userId).SumAsync(s => s.PlayCount);
    }

    public async Task<double> GetTotalListeningForUserAsync(int userId)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        return await db.UserTrackStats.Where(s => s.UserId == userId).SumAsync(s => s.TotalListeningSeconds);
    }

    // === Helper ===

    private async Task<UserTrackStats> GetOrCreateStatAsync(AppDbContext db, int trackId)
    {
        var stat = await db.UserTrackStats.FirstOrDefaultAsync(s => s.UserId == _currentUserId && s.TrackId == trackId);
        if (stat == null)
        {
            stat = new UserTrackStats
            {
                UserId = _currentUserId,
                TrackId = trackId
            };
            db.UserTrackStats.Add(stat);
        }
        return stat;
    }
}
