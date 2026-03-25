using Microsoft.AspNetCore.Components.Forms;
using AycaMusic.Data;
using AycaMusic.Models;
using Microsoft.EntityFrameworkCore;

namespace AycaMusic.Services;

public class TrackUploadService
{
    private readonly IWebHostEnvironment _env;
    private readonly MusicPlayerService _player;
    private const long MaxFileSize = 50 * 1024 * 1024; // 50 MB

    public TrackUploadService(IWebHostEnvironment env, MusicPlayerService player)
    {
        _env = env;
        _player = player;
    }

    /// <summary>
    /// Upload an MP3 file and add it to the database.
    /// Returns (success, errorMessage).
    /// </summary>
    public async Task<(bool Success, string Message)> UploadTrackAsync(
        IBrowserFile file,
        string title,
        string artist,
        string genre,
        string mood,
        string gradient,
        int addedByUserId)
    {
        // Validate title
        if (string.IsNullOrWhiteSpace(title))
            return (false, "Şarkı adı boş olamaz.");

        // Validate file
        if (file == null)
            return (false, "Lütfen bir MP3 dosyası seçin.");

        // Validate extension
        var ext = Path.GetExtension(file.Name).ToLowerInvariant();
        if (ext != ".mp3")
            return (false, "Sadece MP3 dosyaları yüklenebilir.");

        // Validate size
        if (file.Size > MaxFileSize)
            return (false, "Dosya boyutu 50 MB'ı aşamaz.");

        try
        {
            // Sanitize filename
            var safeName = SanitizeFileName(title, artist) + ".mp3";
            var musicDir = Path.Combine(_env.WebRootPath, "music");

            // Ensure directory exists
            if (!Directory.Exists(musicDir))
                Directory.CreateDirectory(musicDir);

            var filePath = Path.Combine(musicDir, safeName);

            // Check for duplicate
            if (File.Exists(filePath))
                safeName = $"{Path.GetFileNameWithoutExtension(safeName)}_{DateTime.UtcNow:yyyyMMddHHmmss}.mp3";
            filePath = Path.Combine(musicDir, safeName);

            // Save file
            await using var stream = file.OpenReadStream(MaxFileSize);
            await using var fileStream = new FileStream(filePath, FileMode.Create);
            await stream.CopyToAsync(fileStream);

            // Add to database via MusicPlayerService
            var relativePath = $"music/{safeName}";
            await _player.AddTrackToDbAsync(
                title.Trim(),
                string.IsNullOrWhiteSpace(artist) ? "Unknown Artist" : artist.Trim(),
                relativePath,
                genre,
                mood,
                gradient,
                "", // iconSvg — empty for uploaded tracks
                addedByUserId);

            return (true, $"'{title}' başarıyla yüklendi!");
        }
        catch (Exception ex)
        {
            return (false, $"Yükleme hatası: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete a track file from disk and database.
    /// </summary>
    public async Task<(bool Success, string Message)> DeleteTrackAsync(int trackId)
    {
        try
        {
            // Find the track's file path from in-memory list
            var track = _player.Tracks.FirstOrDefault(t => t.Id == trackId);
            if (track != null && !string.IsNullOrEmpty(track.Src))
            {
                var fullPath = Path.Combine(_env.WebRootPath, track.Src);
                if (File.Exists(fullPath))
                    File.Delete(fullPath);
            }

            await _player.DeleteTrackFromDbAsync(trackId);
            return (true, "Şarkı silindi.");
        }
        catch (Exception ex)
        {
            return (false, $"Silme hatası: {ex.Message}");
        }
    }

    private static string SanitizeFileName(string title, string artist)
    {
        var name = $"{artist} - {title}";
        // Remove invalid file name characters
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        // Replace whitespace sequences with single space
        name = System.Text.RegularExpressions.Regex.Replace(name, @"\s+", " ").Trim();
        return name;
    }
}
