using Musicly.Data;
using Musicly.Models;
using Microsoft.EntityFrameworkCore;

namespace Musicly.Services;

public enum RepeatMode { Off, All, One }

public class MusicPlayerService
{
    public List<Track> Tracks { get; private set; } = new();
    public int CurrentTrackIndex { get; private set; }
    public bool IsPlaying { get; set; }
    public double CurrentTime { get; set; }
    public double Duration { get; set; }
    public bool IsShuffled { get; set; }
    public RepeatMode Repeat { get; set; } = RepeatMode.Off;
    public bool IsMuted { get; set; }
    public double PreMuteVolume { get; set; } = 0.7;

    // Queue
    public List<Track> Queue { get; private set; } = new();
    public bool ShowQueue { get; set; }

    public event Action? OnStateChanged;

    public Track CurrentTrack => Tracks.Count > 0 ? Tracks[CurrentTrackIndex] : new Track();

    private readonly Random _random = new();
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private bool _tracksLoaded;

    public MusicPlayerService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
        InitializeDefaultTracks(); // Fallback until DB loads
    }

    /// <summary>
    /// Load tracks from database. Called after auth is confirmed.
    /// </summary>
    public async Task LoadTracksFromDbAsync()
    {
        if (_tracksLoaded) return;
        try
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            var dbTracks = await db.Tracks.AsNoTracking().OrderBy(t => t.Id).ToListAsync();
            if (dbTracks.Count > 0)
            {
                var hardcodedLyrics = GetHardcodedLyrics();
                var coverMap = GetCoverImageMap();
                Tracks = dbTracks.Select(dt => {
                    var isYt = dt.FilePath.StartsWith("youtube:");
                    string? ytVideoId = null;
                    string coverImage = "";
                    if (isYt)
                    {
                        var parts = dt.FilePath.Substring("youtube:".Length).Split('|');
                        ytVideoId = parts[0];
                        if (parts.Length > 1)
                        {
                            coverImage = parts[1];
                        }
                    }
                    else
                    {
                        coverImage = coverMap.GetValueOrDefault(dt.Id, "");
                    }
                    return new Track
                    {
                        Id = dt.Id,
                        Title = dt.Title,
                        Artist = dt.Artist,
                        Src = isYt ? "" : dt.FilePath,
                        Genre = dt.Genre,
                        Mood = dt.Mood,
                        GradientColor = dt.GradientColor,
                        IconSvg = dt.IconSvg,
                        CoverImage = coverImage,
                        IsYouTube = isYt,
                        YouTubeVideoId = ytVideoId,
                        Lyrics = isYt ? new List<LyricLine> { new() { Time = 0, Text = "♪ YouTube Music ♪" } } : hardcodedLyrics.GetValueOrDefault(dt.Id, new List<LyricLine> { new() { Time = 0, Text = "♪ ♪" } })
                    };
                }).ToList();
                _tracksLoaded = true;
                NotifyStateChanged();
            }
        }
        catch { }
    }

    /// <summary>
    /// Add a new track (admin). Returns the new track's DB id.
    /// </summary>
    public async Task<int> AddTrackToDbAsync(string title, string artist, string filePath, string genre, string mood, string gradient, string iconSvg, int addedByUserId)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var dbTrack = new DbTrack
        {
            Title = title,
            Artist = artist,
            FilePath = filePath,
            Genre = genre,
            Mood = mood,
            GradientColor = gradient,
            IconSvg = iconSvg,
            CreatedAt = DateTime.UtcNow,
            AddedByUserId = addedByUserId
        };
        db.Tracks.Add(dbTrack);
        await db.SaveChangesAsync();

        // Add to in-memory list
        Tracks.Add(new Track
        {
            Id = dbTrack.Id,
            Title = dbTrack.Title,
            Artist = dbTrack.Artist,
            Src = dbTrack.FilePath,
            Genre = dbTrack.Genre,
            Mood = dbTrack.Mood,
            GradientColor = dbTrack.GradientColor,
            IconSvg = dbTrack.IconSvg,
            Lyrics = new List<LyricLine> { new() { Time = 0, Text = "♪ ♪" } }
        });

        NotifyStateChanged();
        return dbTrack.Id;
    }

    public async Task DeleteTrackFromDbAsync(int trackId)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var dbTrack = await db.Tracks.FindAsync(trackId);
        if (dbTrack != null)
        {
            db.Tracks.Remove(dbTrack);
            await db.SaveChangesAsync();
            Tracks.RemoveAll(t => t.Id == trackId);
            if (CurrentTrackIndex >= Tracks.Count)
                CurrentTrackIndex = 0;
            NotifyStateChanged();
        }
    }

    public async Task<List<DbTrack>> GetAllDbTracksAsync()
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Tracks.AsNoTracking().Include(t => t.AddedBy).OrderBy(t => t.Id).ToListAsync();
    }

    // ========================================
    // Default fallback tracks (used before DB is loaded)
    // ========================================
    private void InitializeDefaultTracks()
    {
        Tracks = new List<Track>
        {
            new Track { Id = 1, Title = "Ashes in Slow Motion", Artist = "Unknown Artist", Src = "music/Ashes in Slow Motion.mp3", Genre = "Cinematic", Mood = "Melancholy", GradientColor = "linear-gradient(135deg, #1a1a2e, #16213e, #0f3460, #e94560)", IconSvg = "<svg viewBox=\"0 0 24 24\" width=\"22\" height=\"22\" fill=\"rgba(255,255,255,0.8)\"><path d=\"M13.5.67s.74 2.65.74 4.8c0 2.06-1.35 3.73-3.41 3.73-2.07 0-3.63-1.67-3.63-3.73l.03-.36C5.21 7.51 4 10.62 4 14c0 4.42 3.58 8 8 8s8-3.58 8-8C20 8.61 17.41 3.8 13.5.67z\"/></svg>", CoverImage = "images/covers/ashes_in_slow_motion.png" },
            new Track { Id = 2, Title = "Eclipsed Tides", Artist = "Unknown Artist", Src = "music/Eclipsed Tides.mp3", Genre = "Ambient", Mood = "Chill", GradientColor = "linear-gradient(135deg, #0c0c1d, #1b2845, #274060, #1b6ca8)", IconSvg = "<svg viewBox=\"0 0 24 24\" width=\"22\" height=\"22\" fill=\"rgba(255,255,255,0.8)\"><path d=\"M12 3v10.55c-.59-.34-1.27-.55-2-.55C7.79 13 6 14.79 6 17s1.79 4 4 4 4-1.79 4-4V7h4V3h-6z\"/></svg>", CoverImage = "images/covers/eclipsed_tides.png" },
        };
    }

    // Lyrics are still hard-coded (not in DB) — they're presentation data
    private Dictionary<int, List<LyricLine>> GetHardcodedLyrics()
    {
        return new Dictionary<int, List<LyricLine>>
        {
            [1] = new() {
                new() { Time = 0, Text = "♪ Intro ♪" }, new() { Time = 8, Text = "Burning through the silence" },
                new() { Time = 14, Text = "Ashes falling down like rain" }, new() { Time = 20, Text = "Every flame a memory" },
                new() { Time = 26, Text = "Every spark a trace of pain" }, new() { Time = 32, Text = "In slow motion, we shatter" },
                new() { Time = 38, Text = "Pieces scattered on the floor" }, new() { Time = 44, Text = "Nothing left but embers" },
                new() { Time = 50, Text = "Of everything we were before" }, new() { Time = 58, Text = "♪  ♪" },
                new() { Time = 66, Text = "Watch the world dissolve away" }, new() { Time = 72, Text = "Into shades of black and grey" },
                new() { Time = 78, Text = "We were fire, we were light" }, new() { Time = 84, Text = "Now we fade into the night" },
                new() { Time = 92, Text = "Ashes in slow motion" }, new() { Time = 98, Text = "Drifting through a broken sky" },
                new() { Time = 104, Text = "Hold me like you used to" }, new() { Time = 110, Text = "Before you taught me how to cry" },
                new() { Time = 118, Text = "♪  ♪" }, new() { Time = 170, Text = "♪ Outro ♪" }
            },
            [2] = new() {
                new() { Time = 0, Text = "♪ Intro ♪" }, new() { Time = 7, Text = "Waves beneath a hidden moon" },
                new() { Time = 13, Text = "Tides that pull me back to you" }, new() { Time = 19, Text = "Eclipsed by shadows, lost at sea" },
                new() { Time = 25, Text = "Searching for what used to be" }, new() { Time = 33, Text = "The ocean speaks in whispers" },
                new() { Time = 39, Text = "Of promises we left behind" }, new() { Time = 45, Text = "Currents carry all our secrets" },
                new() { Time = 51, Text = "To the shores we'll never find" }, new() { Time = 59, Text = "♪  ♪" },
                new() { Time = 67, Text = "Under waves of indigo" }, new() { Time = 73, Text = "Where the deep currents flow" },
                new() { Time = 170, Text = "♪ Outro ♪" }
            },
            [3] = new() {
                new() { Time = 0, Text = "♪ Intro ♪" }, new() { Time = 6, Text = "Raise your glass to the midnight sky" },
                new() { Time = 12, Text = "The fiddles play as the sparks fly high" }, new() { Time = 18, Text = "Stomp your boots on the cobblestone" },
                new() { Time = 24, Text = "Tonight we dance, we are never alone" }, new() { Time = 32, Text = "High octane craic, feel the thunder roll" },
                new() { Time = 38, Text = "Fire in the belly, music in the soul" }, new() { Time = 106, Text = "♪ Outro ♪" }
            },
            [4] = new() {
                new() { Time = 0, Text = "♪ Intro ♪" }, new() { Time = 7, Text = "Rain falls heavy on forgotten streets" },
                new() { Time = 13, Text = "Where history whispers beneath our feet" }, new() { Time = 19, Text = "Crimson rivers in the cracks below" },
                new() { Time = 25, Text = "Stories buried where the shadows grow" }, new() { Time = 33, Text = "Blood in the cobblestones, memories stain" },
                new() { Time = 39, Text = "Every stone a witness to the pain" }, new() { Time = 101, Text = "♪ Outro ♪" }
            },
            [5] = new() {
                new() { Time = 0, Text = "♪ Intro ♪" }, new() { Time = 8, Text = "Digital pulses through the void" },
                new() { Time = 14, Text = "Zerberus guards the gates of noise" }, new() { Time = 20, Text = "One four five, the code ignites" },
                new() { Time = 26, Text = "Synthetic thunder splits the nights" }, new() { Time = 34, Text = "Bass drops deep beneath the floor" },
                new() { Time = 40, Text = "Every beat demands encore" }, new() { Time = 102, Text = "♪ Outro ♪" }
            },
            [6] = new() {
                new() { Time = 0, Text = "♪ Intro ♪" }, new() { Time = 7, Text = "Frozen roads stretch out for miles" },
                new() { Time = 13, Text = "Neon glow through icy tiles" }, new() { Time = 19, Text = "Engine roars beneath the cold" },
                new() { Time = 25, Text = "Pushing limits, breaking the mold" }, new() { Time = 33, Text = "Sub zero velocity, feel the rush" },
                new() { Time = 101, Text = "♪ Outro ♪" }
            },
            [7] = new() {
                new() { Time = 0, Text = "♪ Intro ♪" }, new() { Time = 6, Text = "Lights flash across the stage" },
                new() { Time = 12, Text = "Bassline drops, release the rage" }, new() { Time = 18, Text = "Shockwave hits the runway floor" },
                new() { Time = 24, Text = "Crowd erupts and screams for more" }, new() { Time = 100, Text = "♪ Outro ♪" }
            },
            [8] = new() {
                new() { Time = 0, Text = "♪ Intro ♪" }, new() { Time = 7, Text = "Waves return with different tones" },
                new() { Time = 13, Text = "Remixed echoes, shifting zones" }, new() { Time = 19, Text = "The tide comes back in deeper blue" },
                new() { Time = 25, Text = "A different shade of missing you" }, new() { Time = 101, Text = "♪ Outro ♪" }
            },
            [9] = new() {
                new() { Time = 0, Text = "♪ Intro ♪" }, new() { Time = 8, Text = "Fire rains from blackened skies" },
                new() { Time = 14, Text = "No one listens to the cries" }, new() { Time = 20, Text = "Chains of sin around our throats" },
                new() { Time = 26, Text = "Burning bridges, sinking boats" }, new() { Time = 34, Text = "Everyone belongs to hell" },
                new() { Time = 40, Text = "Every demon knows us well" }, new() { Time = 108, Text = "♪ Outro ♪" }
            },
        };
    }

    /// <summary>Cover image map for seed tracks</summary>
    private static Dictionary<int, string> GetCoverImageMap() => new()
    {
        [1] = "images/covers/ashes_in_slow_motion.png",
        [2] = "images/covers/eclipsed_tides.png",
        [3] = "images/covers/high_octane_craic.png",
        [4] = "images/covers/blood_cobblestones.png",
        [5] = "images/covers/zerberus_145.png",
        [6] = "images/covers/sub_zero_velocity.png",
        [7] = "images/covers/shockwave_runway.png",
        [8] = "images/covers/eclipsed_tides_remix.png",
        [9] = "images/covers/everyone_belongs_hell.png",
    };

    // ========================================
    // Playback controls (unchanged)
    // ========================================

    public void LoadTrack(int index)
    {
        if (index >= 0 && index < Tracks.Count)
        {
            CurrentTrackIndex = index;
            CurrentTime = 0;
            Duration = 0;
            NotifyStateChanged();
        }
    }

    public void ToggleShuffle()
    {
        IsShuffled = !IsShuffled;
        NotifyStateChanged();
    }

    public void CycleRepeat()
    {
        Repeat = Repeat switch
        {
            RepeatMode.Off => RepeatMode.All,
            RepeatMode.All => RepeatMode.One,
            RepeatMode.One => RepeatMode.Off,
            _ => RepeatMode.Off
        };
        NotifyStateChanged();
    }

    public void ToggleMute()
    {
        IsMuted = !IsMuted;
        NotifyStateChanged();
    }

    public int GetNextTrackIndex()
    {
        if (Repeat == RepeatMode.One)
            return CurrentTrackIndex;

        if (IsShuffled)
        {
            if (Tracks.Count <= 1) return 0;
            int next;
            do { next = _random.Next(Tracks.Count); }
            while (next == CurrentTrackIndex);
            return next;
        }

        var nextIdx = CurrentTrackIndex + 1;
        if (nextIdx >= Tracks.Count)
        {
            return Repeat == RepeatMode.All ? 0 : -1;
        }
        return nextIdx;
    }

    public void NextTrack()
    {
        var next = GetNextTrackIndex();
        if (next >= 0)
            LoadTrack(next);
    }

    public void PrevTrack()
    {
        if (CurrentTime > 3)
        {
            CurrentTime = 0;
            NotifyStateChanged();
        }
        else
        {
            var prev = CurrentTrackIndex > 0 ? CurrentTrackIndex - 1 : Tracks.Count - 1;
            LoadTrack(prev);
        }
    }

    public void UpdateTime(double time, double duration)
    {
        CurrentTime = time;
        Duration = duration;
        NotifyStateChanged();
    }

    public void SetPlaying(bool playing)
    {
        IsPlaying = playing;
        NotifyStateChanged();
    }

    public void NotifyStateChanged() => OnStateChanged?.Invoke();

    // --- Explore & Radio helpers ---
    public List<string> GetAllGenres() => Tracks.Select(t => t.Genre).Distinct().ToList();
    public List<string> GetAllMoods() => Tracks.Select(t => t.Mood).Distinct().ToList();

    public List<Track> GetTracksByGenre(string genre) =>
        string.IsNullOrEmpty(genre) ? Tracks : Tracks.Where(t => t.Genre.Equals(genre, StringComparison.OrdinalIgnoreCase)).ToList();

    public List<Track> GetTracksByMood(string mood) =>
        string.IsNullOrEmpty(mood) ? Tracks : Tracks.Where(t => t.Mood.Equals(mood, StringComparison.OrdinalIgnoreCase)).ToList();

    public List<Track> SearchTracks(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return Tracks;
        var q = query.Trim().ToLowerInvariant();
        return Tracks.Where(t =>
            t.Title.ToLowerInvariant().Contains(q) ||
            t.Artist.ToLowerInvariant().Contains(q) ||
            t.Genre.ToLowerInvariant().Contains(q)
        ).ToList();
    }

    public void PlayRandomFromGenre(string genre)
    {
        var genreTracks = GetTracksByGenre(genre);
        if (genreTracks.Count == 0) genreTracks = Tracks;
        var idx = _random.Next(genreTracks.Count);
        var track = genreTracks[idx];
        var realIdx = Tracks.IndexOf(track);
        if (realIdx >= 0)
        {
            IsShuffled = true;
            LoadTrack(realIdx);
        }
    }

    // ========================================
    // Queue Management
    // ========================================

    public void AddToQueue(Track track)
    {
        if (!Queue.Any(t => t.Id == track.Id))
        {
            Queue.Add(track);
            NotifyStateChanged();
        }
    }

    public void RemoveFromQueue(int trackId)
    {
        Queue.RemoveAll(t => t.Id == trackId);
        NotifyStateChanged();
    }

    public void ClearQueue()
    {
        Queue.Clear();
        NotifyStateChanged();
    }

    public Track? PlayFromQueue()
    {
        if (Queue.Count == 0) return null;
        var track = Queue[0];
        Queue.RemoveAt(0);
        var idx = Tracks.IndexOf(track);
        if (idx >= 0) LoadTrack(idx);
        return track;
    }

    public void ToggleQueue()
    {
        ShowQueue = !ShowQueue;
        NotifyStateChanged();
    }

    // ========================================
    // YouTube Integration
    // ========================================

    /// <summary>
    /// Load a YouTube track for playback. Adds it as a temporary track.
    /// </summary>
    public void LoadYouTubeTrack(Models.YouTubeSearchResult result)
    {
        // Check if already in the list
        var existing = Tracks.FindIndex(t => t.IsYouTube && t.YouTubeVideoId == result.VideoId);
        if (existing >= 0)
        {
            LoadTrack(existing);
            return;
        }

        // Create a temporary track from YouTube result
        var ytTrack = new Track
        {
            Id = -(Tracks.Count + 1000), // Negative ID to avoid DB conflicts
            Title = result.Title,
            Artist = result.ChannelName,
            Src = "", // No MP3 source
            Genre = "YouTube",
            Mood = "YouTube",
            GradientColor = "linear-gradient(135deg, #ff0000, #cc0000, #990000, #660000)",
            CoverImage = result.ThumbnailUrl,
            IsYouTube = true,
            YouTubeVideoId = result.VideoId,
            Lyrics = new List<LyricLine> { new() { Time = 0, Text = "♪ YouTube Music ♪" } }
        };

        Tracks.Add(ytTrack);
        LoadTrack(Tracks.Count - 1);
    }

    /// <summary>
    /// Check if the current track is a YouTube track.
    /// </summary>
    public bool IsCurrentTrackYouTube => CurrentTrack.IsYouTube;

    // ========================================
    // YouTube Integration
    // ========================================
    public async Task<int> EnsureYouTubeTrackSavedAsync(Track track)
    {
        if (!track.IsYouTube) return track.Id;
        if (track.Id > 0) return track.Id;

        using var db = await _dbFactory.CreateDbContextAsync();
        
        var youtubePath = $"youtube:{track.YouTubeVideoId}|{track.CoverImage}";
        var existingDbTrack = await db.Tracks.FirstOrDefaultAsync(t => t.FilePath.StartsWith($"youtube:{track.YouTubeVideoId}|") || t.FilePath == $"youtube:{track.YouTubeVideoId}");
        
        if (existingDbTrack != null)
        {
            track.Id = existingDbTrack.Id;
            NotifyStateChanged();
            return track.Id;
        }

        var dbTrack = new DbTrack
        {
            Title = track.Title,
            Artist = track.Artist,
            FilePath = youtubePath,
            Genre = track.Genre,
            Mood = track.Mood,
            GradientColor = track.GradientColor,
            IconSvg = track.IconSvg,
            CreatedAt = DateTime.UtcNow,
            AddedByUserId = null
        };

        db.Tracks.Add(dbTrack);
        await db.SaveChangesAsync();

        track.Id = dbTrack.Id;
        NotifyStateChanged();
        return track.Id;
    }

    // ========================================
    // Crossfade
    // ========================================
    public int CrossfadeDuration { get; set; } = 0; // 0 = off, 1-5 seconds
}
