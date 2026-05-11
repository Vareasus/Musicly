namespace Musicly.Models;

public enum RequestStatus
{
    Pending,    // Beklemede
    Approved,   // Onaylandı
    Rejected    // Reddedildi
}

public enum RequestCategory
{
    Song,       // Şarkı Talebi
    Feature,    // Özellik Önerisi
    Bug,        // Hata Bildirimi
    Other       // Diğer
}

public class SongRequest
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = "";
    public RequestCategory Category { get; set; } = RequestCategory.Song;
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public RequestStatus Status { get; set; } = RequestStatus.Pending;
    public string? AdminResponse { get; set; }
    public int? RespondedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RespondedAt { get; set; }

    // Keep old fields for backward compat (mapped from Title)
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string SongTitle => Title;
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string ArtistName => "";
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string? Message => Description;

    // Navigation
    public AppUser? User { get; set; }
    public AppUser? RespondedBy { get; set; }
}
