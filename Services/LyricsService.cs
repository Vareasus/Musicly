using System.Text.Json;
using System.Text.RegularExpressions;
using Musicly.Models;

namespace Musicly.Services;

/// <summary>
/// Fetches synced and plain lyrics from lrclib.net (free, no API key needed).
/// </summary>
public class LyricsService
{
    private readonly HttpClient _http;
    private const string BaseUrl = "https://lrclib.net/api";

    public LyricsService(HttpClient http)
    {
        _http = http;
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("Musicly/1.0");
    }

    /// <summary>
    /// Search for synced lyrics by track title and artist.
    /// Returns a list of LyricLine with timestamps, or plain lyrics if synced not available.
    /// </summary>
    public async Task<List<LyricLine>> GetLyricsAsync(string title, string artist)
    {
        if (string.IsNullOrWhiteSpace(title)) return new();

        try
        {
            // Clean up title — remove common YouTube suffixes
            var cleanTitle = CleanTitle(title);
            var cleanArtist = CleanArtist(artist);

            // Try searching with artist and title
            var lyrics = await TryGetLyrics(cleanTitle, cleanArtist);
            if (lyrics.Count > 0) return lyrics;

            // Try with just the title (sometimes artist is embedded)
            lyrics = await TryGetLyrics(cleanTitle, "");
            if (lyrics.Count > 0) return lyrics;

            // Try the original title as-is
            if (cleanTitle != title)
            {
                lyrics = await TryGetLyrics(title, "");
                if (lyrics.Count > 0) return lyrics;
            }

            return new List<LyricLine> { new() { Time = 0, Text = "🎵 Lyrics not found" } };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Lyrics fetch error: {ex.Message}");
            return new List<LyricLine> { new() { Time = 0, Text = "🎵 Lyrics not available" } };
        }
    }

    private async Task<List<LyricLine>> TryGetLyrics(string title, string artist)
    {
        var query = string.IsNullOrWhiteSpace(artist)
            ? $"track_name={Uri.EscapeDataString(title)}"
            : $"track_name={Uri.EscapeDataString(title)}&artist_name={Uri.EscapeDataString(artist)}";

        var url = $"{BaseUrl}/search?{query}";
        var response = await _http.GetAsync(url);

        if (!response.IsSuccessStatusCode) return new();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var results = doc.RootElement;

        if (results.GetArrayLength() == 0) return new();

        // Find the best match — prefer one with synced lyrics
        JsonElement? bestMatch = null;
        foreach (var item in results.EnumerateArray())
        {
            if (item.TryGetProperty("syncedLyrics", out var synced) && synced.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(synced.GetString()))
            {
                bestMatch = item;
                break;
            }
            bestMatch ??= item;
        }

        if (bestMatch == null) return new();

        // Try synced lyrics first (LRC format with timestamps)
        if (bestMatch.Value.TryGetProperty("syncedLyrics", out var syncedLyrics) && syncedLyrics.ValueKind == JsonValueKind.String)
        {
            var lrc = syncedLyrics.GetString();
            if (!string.IsNullOrWhiteSpace(lrc))
            {
                var parsed = ParseLrc(lrc);
                if (parsed.Count > 0) return parsed;
            }
        }

        // Fall back to plain lyrics
        if (bestMatch.Value.TryGetProperty("plainLyrics", out var plainLyrics) && plainLyrics.ValueKind == JsonValueKind.String)
        {
            var plain = plainLyrics.GetString();
            if (!string.IsNullOrWhiteSpace(plain))
            {
                return ParsePlainLyrics(plain);
            }
        }

        return new();
    }

    /// <summary>
    /// Parse LRC format: [mm:ss.xx] Lyric text
    /// </summary>
    private static List<LyricLine> ParseLrc(string lrc)
    {
        var lines = new List<LyricLine>();
        var pattern = new Regex(@"\[(\d+):(\d+)\.(\d+)\]\s*(.*)");

        foreach (var rawLine in lrc.Split('\n'))
        {
            var match = pattern.Match(rawLine.Trim());
            if (match.Success)
            {
                var minutes = int.Parse(match.Groups[1].Value);
                var seconds = int.Parse(match.Groups[2].Value);
                var text = match.Groups[4].Value.Trim();

                if (string.IsNullOrWhiteSpace(text)) text = "♪";

                var totalSeconds = minutes * 60 + seconds;
                lines.Add(new LyricLine { Time = totalSeconds, Text = text });
            }
        }

        return lines.OrderBy(l => l.Time).ToList();
    }

    /// <summary>
    /// Convert plain lyrics (no timestamps) to LyricLine list.
    /// Each line gets a 0 timestamp (won't auto-scroll, but still displays).
    /// </summary>
    private static List<LyricLine> ParsePlainLyrics(string plain)
    {
        var lines = new List<LyricLine>();
        var lineTexts = plain.Split('\n');
        var timePerLine = 4; // rough estimate: 4 seconds per line

        for (int i = 0; i < lineTexts.Length; i++)
        {
            var text = lineTexts[i].Trim();
            if (string.IsNullOrWhiteSpace(text)) text = "♪";
            lines.Add(new LyricLine { Time = i * timePerLine, Text = text });
        }

        return lines;
    }

    /// <summary>
    /// Clean YouTube video title to extract song name.
    /// Removes "(Official Video)", "MV", "Lyrics", etc.
    /// </summary>
    private static string CleanTitle(string title)
    {
        // Remove common YouTube suffixes
        var patterns = new[]
        {
            @"\(Official\s*(Music\s*)?Video\)",
            @"\(Official\s*Audio\)",
            @"\(Lyric\s*Video\)",
            @"\(Lyrics?\)",
            @"\[Official\s*(Music\s*)?Video\]",
            @"\[Official\s*Audio\]",
            @"\[Lyric\s*Video\]",
            @"\[Lyrics?\]",
            @"\(Visualizer\)",
            @"\(Audio\)",
            @"\(MV\)",
            @"\bMV\b",
            @"\bHD\b",
            @"\b4K\b",
            @"\bft\.?\s*",
            @"\bfeat\.?\s*",
            @"\|.*$",  // Remove everything after |
        };

        var result = title;
        foreach (var p in patterns)
        {
            result = Regex.Replace(result, p, "", RegexOptions.IgnoreCase);
        }

        // If title contains " - ", split and use the second part as the song name
        // (first part is usually the artist)
        // But keep the full cleaned title for searching
        return result.Trim().Trim('-').Trim();
    }

    /// <summary>
    /// Clean channel/artist name.
    /// Removes "VEVO", "Official", "Topic" suffixes.
    /// </summary>
    private static string CleanArtist(string artist)
    {
        if (string.IsNullOrWhiteSpace(artist)) return "";

        var patterns = new[]
        {
            @"\s*VEVO$",
            @"\s*-\s*Topic$",
            @"\s*Official$",
            @"\s*Music$",
        };

        var result = artist;
        foreach (var p in patterns)
        {
            result = Regex.Replace(result, p, "", RegexOptions.IgnoreCase);
        }

        return result.Trim();
    }
}
