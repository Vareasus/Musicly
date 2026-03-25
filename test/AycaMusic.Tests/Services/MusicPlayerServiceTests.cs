using AycaMusic.Data;
using AycaMusic.Models;
using AycaMusic.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace AycaMusic.Tests.Services;

public class MusicPlayerServiceTests : IDisposable
{
    private readonly MusicPlayerService _service;
    private readonly Mock<IDbContextFactory<AppDbContext>> _mockDbFactory;

    public MusicPlayerServiceTests()
    {
        _mockDbFactory = new Mock<IDbContextFactory<AppDbContext>>();
        _service = new MusicPlayerService(_mockDbFactory.Object);
    }

    public void Dispose() { }

    // Helper to set up tracks for testing
    private void SetupTracks(int count = 5)
    {
        // The constructor already initializes 2 default tracks.
        // For tests that need more, we add additional tracks.
        for (int i = 3; i <= count; i++)
        {
            _service.Tracks.Add(new Track
            {
                Id = i,
                Title = $"Track {i}",
                Artist = $"Artist {i}",
                Src = $"music/track{i}.mp3",
                Genre = i % 2 == 0 ? "Electronic" : "Ambient",
                Mood = i % 2 == 0 ? "Energetic" : "Chill"
            });
        }
    }

    // ===== Track Initialization =====

    [Fact]
    public void Constructor_ShouldInitializeDefaultTracks()
    {
        Assert.NotNull(_service.Tracks);
        Assert.Equal(2, _service.Tracks.Count); // 2 default fallback tracks
    }

    [Fact]
    public void CurrentTrack_ShouldReturnFirstTrackByDefault()
    {
        Assert.Equal("Ashes in Slow Motion", _service.CurrentTrack.Title);
    }

    [Fact]
    public void CurrentTrackIndex_ShouldBeZeroByDefault()
    {
        Assert.Equal(0, _service.CurrentTrackIndex);
    }

    // ===== LoadTrack =====

    [Fact]
    public void LoadTrack_ValidIndex_ShouldChangeCurrentTrackIndex()
    {
        _service.LoadTrack(1);
        Assert.Equal(1, _service.CurrentTrackIndex);
    }

    [Fact]
    public void LoadTrack_ValidIndex_ShouldResetCurrentTime()
    {
        _service.CurrentTime = 42;
        _service.LoadTrack(1);
        Assert.Equal(0, _service.CurrentTime);
    }

    [Fact]
    public void LoadTrack_ValidIndex_ShouldResetDuration()
    {
        _service.Duration = 180;
        _service.LoadTrack(1);
        Assert.Equal(0, _service.Duration);
    }

    [Fact]
    public void LoadTrack_NegativeIndex_ShouldNotChange()
    {
        _service.LoadTrack(-1);
        Assert.Equal(0, _service.CurrentTrackIndex);
    }

    [Fact]
    public void LoadTrack_IndexOutOfBounds_ShouldNotChange()
    {
        _service.LoadTrack(100);
        Assert.Equal(0, _service.CurrentTrackIndex);
    }

    // ===== ToggleShuffle =====

    [Fact]
    public void ToggleShuffle_ShouldToggleIsShuffled()
    {
        Assert.False(_service.IsShuffled);
        _service.ToggleShuffle();
        Assert.True(_service.IsShuffled);
        _service.ToggleShuffle();
        Assert.False(_service.IsShuffled);
    }

    // ===== CycleRepeat =====

    [Fact]
    public void CycleRepeat_ShouldCycleOffAllOneOff()
    {
        Assert.Equal(RepeatMode.Off, _service.Repeat);

        _service.CycleRepeat();
        Assert.Equal(RepeatMode.All, _service.Repeat);

        _service.CycleRepeat();
        Assert.Equal(RepeatMode.One, _service.Repeat);

        _service.CycleRepeat();
        Assert.Equal(RepeatMode.Off, _service.Repeat);
    }

    // ===== ToggleMute =====

    [Fact]
    public void ToggleMute_ShouldToggleIsMuted()
    {
        Assert.False(_service.IsMuted);
        _service.ToggleMute();
        Assert.True(_service.IsMuted);
        _service.ToggleMute();
        Assert.False(_service.IsMuted);
    }

    // ===== GetNextTrackIndex =====

    [Fact]
    public void GetNextTrackIndex_Normal_ShouldReturnNextIndex()
    {
        SetupTracks(5);
        _service.LoadTrack(0);
        Assert.Equal(1, _service.GetNextTrackIndex());
    }

    [Fact]
    public void GetNextTrackIndex_LastTrack_RepeatOff_ShouldReturnMinusOne()
    {
        // 2 default tracks, index 1 is last
        _service.LoadTrack(1);
        _service.Repeat = RepeatMode.Off;
        Assert.Equal(-1, _service.GetNextTrackIndex());
    }

    [Fact]
    public void GetNextTrackIndex_LastTrack_RepeatAll_ShouldReturnZero()
    {
        _service.LoadTrack(1);
        _service.Repeat = RepeatMode.All;
        Assert.Equal(0, _service.GetNextTrackIndex());
    }

    [Fact]
    public void GetNextTrackIndex_RepeatOne_ShouldReturnCurrentIndex()
    {
        _service.LoadTrack(0);
        _service.Repeat = RepeatMode.One;
        Assert.Equal(0, _service.GetNextTrackIndex());
    }

    [Fact]
    public void GetNextTrackIndex_Shuffled_ShouldReturnDifferentIndex()
    {
        SetupTracks(10);
        _service.IsShuffled = true;
        _service.LoadTrack(0);

        // Run multiple times to verify it doesn't always return the same track
        var results = new HashSet<int>();
        for (int i = 0; i < 50; i++)
        {
            results.Add(_service.GetNextTrackIndex());
        }
        // Should never return the current index (0)
        Assert.DoesNotContain(0, results);
        // With 10 tracks and 50 attempts, should get at least 2 different values
        Assert.True(results.Count >= 2);
    }

    [Fact]
    public void GetNextTrackIndex_Shuffled_SingleTrack_ShouldReturnZero()
    {
        // Remove all tracks and add just one
        _service.Tracks.Clear();
        _service.Tracks.Add(new Track { Id = 1, Title = "Only One" });
        _service.IsShuffled = true;
        Assert.Equal(0, _service.GetNextTrackIndex());
    }

    // ===== NextTrack =====

    [Fact]
    public void NextTrack_ShouldAdvanceToNextTrack()
    {
        _service.LoadTrack(0);
        _service.NextTrack();
        Assert.Equal(1, _service.CurrentTrackIndex);
    }

    [Fact]
    public void NextTrack_AtEnd_RepeatOff_ShouldStayAtLast()
    {
        _service.LoadTrack(1);
        _service.Repeat = RepeatMode.Off;
        _service.NextTrack();
        // GetNextTrackIndex returns -1, so LoadTrack is not called
        Assert.Equal(1, _service.CurrentTrackIndex);
    }

    // ===== PrevTrack =====

    [Fact]
    public void PrevTrack_UnderThreeSeconds_ShouldGoToPreviousTrack()
    {
        _service.LoadTrack(1);
        _service.CurrentTime = 1; // under 3 seconds
        _service.PrevTrack();
        Assert.Equal(0, _service.CurrentTrackIndex);
    }

    [Fact]
    public void PrevTrack_OverThreeSeconds_ShouldResetCurrentTime()
    {
        _service.LoadTrack(1);
        _service.CurrentTime = 5; // over 3 seconds
        _service.PrevTrack();
        Assert.Equal(0, _service.CurrentTime);
        Assert.Equal(1, _service.CurrentTrackIndex); // stays on same track
    }

    [Fact]
    public void PrevTrack_AtFirstTrack_ShouldWrapToLastTrack()
    {
        _service.LoadTrack(0);
        _service.CurrentTime = 0;
        _service.PrevTrack();
        Assert.Equal(1, _service.CurrentTrackIndex); // wraps to last
    }

    // ===== UpdateTime =====

    [Fact]
    public void UpdateTime_ShouldUpdateCurrentTimeAndDuration()
    {
        _service.UpdateTime(42.5, 180.0);
        Assert.Equal(42.5, _service.CurrentTime);
        Assert.Equal(180.0, _service.Duration);
    }

    // ===== SetPlaying =====

    [Fact]
    public void SetPlaying_ShouldToggleIsPlaying()
    {
        _service.SetPlaying(true);
        Assert.True(_service.IsPlaying);
        _service.SetPlaying(false);
        Assert.False(_service.IsPlaying);
    }

    // ===== SearchTracks =====

    [Fact]
    public void SearchTracks_EmptyQuery_ShouldReturnAllTracks()
    {
        var result = _service.SearchTracks("");
        Assert.Equal(_service.Tracks.Count, result.Count);
    }

    [Fact]
    public void SearchTracks_WhitespaceQuery_ShouldReturnAllTracks()
    {
        var result = _service.SearchTracks("   ");
        Assert.Equal(_service.Tracks.Count, result.Count);
    }

    [Fact]
    public void SearchTracks_ByTitle_ShouldFindTrack()
    {
        var result = _service.SearchTracks("Ashes");
        Assert.Contains(result, t => t.Title.Contains("Ashes"));
    }

    [Fact]
    public void SearchTracks_ByGenre_ShouldFindTrack()
    {
        var result = _service.SearchTracks("Cinematic");
        Assert.Contains(result, t => t.Genre == "Cinematic");
    }

    [Fact]
    public void SearchTracks_CaseInsensitive_ShouldMatch()
    {
        var result = _service.SearchTracks("ashes");
        Assert.Contains(result, t => t.Title.Contains("Ashes"));
    }

    [Fact]
    public void SearchTracks_NoMatch_ShouldReturnEmpty()
    {
        var result = _service.SearchTracks("xyznonexistent");
        Assert.Empty(result);
    }

    // ===== GetTracksByGenre =====

    [Fact]
    public void GetTracksByGenre_EmptyGenre_ShouldReturnAllTracks()
    {
        var result = _service.GetTracksByGenre("");
        Assert.Equal(_service.Tracks.Count, result.Count);
    }

    [Fact]
    public void GetTracksByGenre_ValidGenre_ShouldFilterCorrectly()
    {
        SetupTracks(5);
        var result = _service.GetTracksByGenre("Ambient");
        Assert.All(result, t => Assert.Equal("Ambient", t.Genre));
    }

    [Fact]
    public void GetTracksByGenre_CaseInsensitive_ShouldMatch()
    {
        var result = _service.GetTracksByGenre("ambient");
        Assert.All(result, t => Assert.Equal("Ambient", t.Genre, StringComparer.OrdinalIgnoreCase));
    }

    // ===== GetTracksByMood =====

    [Fact]
    public void GetTracksByMood_EmptyMood_ShouldReturnAllTracks()
    {
        var result = _service.GetTracksByMood("");
        Assert.Equal(_service.Tracks.Count, result.Count);
    }

    [Fact]
    public void GetTracksByMood_ValidMood_ShouldFilterCorrectly()
    {
        SetupTracks(5);
        var result = _service.GetTracksByMood("Chill");
        Assert.All(result, t => Assert.Equal("Chill", t.Mood));
    }

    // ===== GetAllGenres / GetAllMoods =====

    [Fact]
    public void GetAllGenres_ShouldReturnDistinctGenres()
    {
        SetupTracks(5);
        var genres = _service.GetAllGenres();
        Assert.Equal(genres.Distinct().Count(), genres.Count);
    }

    [Fact]
    public void GetAllMoods_ShouldReturnDistinctMoods()
    {
        SetupTracks(5);
        var moods = _service.GetAllMoods();
        Assert.Equal(moods.Distinct().Count(), moods.Count);
    }

    // ===== Queue Management =====

    [Fact]
    public void AddToQueue_ShouldAddTrackToQueue()
    {
        var track = _service.Tracks[0];
        _service.AddToQueue(track);
        Assert.Single(_service.Queue);
        Assert.Equal(track.Id, _service.Queue[0].Id);
    }

    [Fact]
    public void AddToQueue_DuplicateTrack_ShouldNotAddAgain()
    {
        var track = _service.Tracks[0];
        _service.AddToQueue(track);
        _service.AddToQueue(track);
        Assert.Single(_service.Queue);
    }

    [Fact]
    public void RemoveFromQueue_ShouldRemoveTrackById()
    {
        var track = _service.Tracks[0];
        _service.AddToQueue(track);
        _service.RemoveFromQueue(track.Id);
        Assert.Empty(_service.Queue);
    }

    [Fact]
    public void RemoveFromQueue_NonExistentId_ShouldDoNothing()
    {
        _service.AddToQueue(_service.Tracks[0]);
        _service.RemoveFromQueue(999);
        Assert.Single(_service.Queue);
    }

    [Fact]
    public void ClearQueue_ShouldEmptyTheQueue()
    {
        _service.AddToQueue(_service.Tracks[0]);
        _service.AddToQueue(_service.Tracks[1]);
        _service.ClearQueue();
        Assert.Empty(_service.Queue);
    }

    [Fact]
    public void PlayFromQueue_EmptyQueue_ShouldReturnNull()
    {
        var result = _service.PlayFromQueue();
        Assert.Null(result);
    }

    [Fact]
    public void PlayFromQueue_ShouldReturnFirstTrackAndRemoveFromQueue()
    {
        _service.AddToQueue(_service.Tracks[0]);
        _service.AddToQueue(_service.Tracks[1]);

        var played = _service.PlayFromQueue();

        Assert.NotNull(played);
        Assert.Equal(_service.Tracks[0].Id, played.Id);
        Assert.Single(_service.Queue); // only second track remains
    }

    [Fact]
    public void ToggleQueue_ShouldToggleShowQueue()
    {
        Assert.False(_service.ShowQueue);
        _service.ToggleQueue();
        Assert.True(_service.ShowQueue);
        _service.ToggleQueue();
        Assert.False(_service.ShowQueue);
    }

    // ===== OnStateChanged Event =====

    [Fact]
    public void LoadTrack_ShouldFireOnStateChanged()
    {
        var fired = false;
        _service.OnStateChanged += () => fired = true;
        _service.LoadTrack(1);
        Assert.True(fired);
    }

    [Fact]
    public void ToggleShuffle_ShouldFireOnStateChanged()
    {
        var fired = false;
        _service.OnStateChanged += () => fired = true;
        _service.ToggleShuffle();
        Assert.True(fired);
    }

    [Fact]
    public void CycleRepeat_ShouldFireOnStateChanged()
    {
        var fired = false;
        _service.OnStateChanged += () => fired = true;
        _service.CycleRepeat();
        Assert.True(fired);
    }

    // ===== Crossfade =====

    [Fact]
    public void CrossfadeDuration_DefaultShouldBeZero()
    {
        Assert.Equal(0, _service.CrossfadeDuration);
    }

    [Fact]
    public void CrossfadeDuration_ShouldBeSettable()
    {
        _service.CrossfadeDuration = 3;
        Assert.Equal(3, _service.CrossfadeDuration);
    }

    // ===== PreMuteVolume =====

    [Fact]
    public void PreMuteVolume_DefaultShouldBe07()
    {
        Assert.Equal(0.7, _service.PreMuteVolume);
    }
}
