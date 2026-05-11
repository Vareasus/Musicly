using System.Text.Json;
using Musicly.Models;

namespace Musicly.Services;

public class YouTubeService
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private const string SearchUrl = "https://www.googleapis.com/youtube/v3/search";
    private const string VideoUrl = "https://www.googleapis.com/youtube/v3/videos";

    public YouTubeService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _apiKey = config["YouTube:ApiKey"] ?? "";
    }

    /// <summary>
    /// Search YouTube for music videos.
    /// </summary>
    public async Task<List<YouTubeSearchResult>> SearchAsync(string query, int maxResults = 12)
    {
        if (string.IsNullOrWhiteSpace(query) || string.IsNullOrEmpty(_apiKey))
            return new();

        try
        {
            // Step 1: Search for videos
            var searchUrl = $"{SearchUrl}?part=snippet&q={Uri.EscapeDataString(query + " music")}&type=video&videoCategoryId=10&maxResults={maxResults}&key={_apiKey}";
            var searchJson = await _http.GetStringAsync(searchUrl);
            using var searchDoc = JsonDocument.Parse(searchJson);

            var items = searchDoc.RootElement.GetProperty("items");
            var videoIds = new List<string>();
            var snippets = new Dictionary<string, (string Title, string Channel, string Thumbnail)>();

            foreach (var item in items.EnumerateArray())
            {
                var videoId = item.GetProperty("id").GetProperty("videoId").GetString() ?? "";
                var snippet = item.GetProperty("snippet");
                var title = snippet.GetProperty("title").GetString() ?? "";
                var channel = snippet.GetProperty("channelTitle").GetString() ?? "";

                // Get best thumbnail
                var thumbs = snippet.GetProperty("thumbnails");
                var thumbUrl = "";
                if (thumbs.TryGetProperty("high", out var highThumb))
                    thumbUrl = highThumb.GetProperty("url").GetString() ?? "";
                else if (thumbs.TryGetProperty("medium", out var medThumb))
                    thumbUrl = medThumb.GetProperty("url").GetString() ?? "";
                else if (thumbs.TryGetProperty("default", out var defThumb))
                    thumbUrl = defThumb.GetProperty("url").GetString() ?? "";

                // Decode HTML entities in title
                title = System.Net.WebUtility.HtmlDecode(title);
                channel = System.Net.WebUtility.HtmlDecode(channel);

                videoIds.Add(videoId);
                snippets[videoId] = (title, channel, thumbUrl);
            }

            if (videoIds.Count == 0) return new();

            // Step 2: Get video durations
            var idsParam = string.Join(",", videoIds);
            var videoDetailsUrl = $"{VideoUrl}?part=contentDetails&id={idsParam}&key={_apiKey}";
            var detailsJson = await _http.GetStringAsync(videoDetailsUrl);
            using var detailsDoc = JsonDocument.Parse(detailsJson);

            var durations = new Dictionary<string, (string Formatted, int Seconds)>();
            foreach (var item in detailsDoc.RootElement.GetProperty("items").EnumerateArray())
            {
                var id = item.GetProperty("id").GetString() ?? "";
                var iso = item.GetProperty("contentDetails").GetProperty("duration").GetString() ?? "";
                var (formatted, seconds) = ParseIsoDuration(iso);
                durations[id] = (formatted, seconds);
            }

            // Combine results
            var results = new List<YouTubeSearchResult>();
            foreach (var vid in videoIds)
            {
                if (!snippets.ContainsKey(vid)) continue;
                var s = snippets[vid];
                var d = durations.ContainsKey(vid) ? durations[vid] : (Formatted: "?:??", Seconds: 0);
                results.Add(new YouTubeSearchResult
                {
                    VideoId = vid,
                    Title = s.Title,
                    ChannelName = s.Channel,
                    ThumbnailUrl = s.Thumbnail,
                    Duration = d.Formatted,
                    DurationSeconds = d.Seconds
                });
            }

            return results;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"YouTube search error: {ex.Message}");
            return new();
        }
    }

    /// <summary>
    /// Parse ISO 8601 duration (PT3M45S) to "3:45" format and total seconds.
    /// </summary>
    private static (string Formatted, int Seconds) ParseIsoDuration(string iso)
    {
        try
        {
            var duration = System.Xml.XmlConvert.ToTimeSpan(iso);
            var totalSeconds = (int)duration.TotalSeconds;
            if (duration.TotalHours >= 1)
                return ($"{(int)duration.TotalHours}:{duration.Minutes:D2}:{duration.Seconds:D2}", totalSeconds);
            return ($"{duration.Minutes}:{duration.Seconds:D2}", totalSeconds);
        }
        catch
        {
            return ("?:??", 0);
        }
    }
}
