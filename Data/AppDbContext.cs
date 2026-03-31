using Microsoft.EntityFrameworkCore;
using Musicly.Models;
using System.Security.Cryptography;
using System.Text;

namespace Musicly.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<DbTrack> Tracks => Set<DbTrack>();
    public DbSet<UserTrackStats> UserTrackStats => Set<UserTrackStats>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Playlist> Playlists => Set<Playlist>();
    public DbSet<PlaylistTrack> PlaylistTracks => Set<PlaylistTrack>();
    public DbSet<ListeningHistory> ListeningHistory => Set<ListeningHistory>();
    public DbSet<TrackComment> TrackComments => Set<TrackComment>();
    public DbSet<CommentLike> CommentLikes => Set<CommentLike>();
    public DbSet<SongRequest> SongRequests => Set<SongRequest>();
    public DbSet<PrivateMessage> PrivateMessages => Set<PrivateMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // === AppUser ===
        modelBuilder.Entity<AppUser>(e =>
        {
            e.HasIndex(u => u.Username).IsUnique();
            e.HasIndex(u => u.Email).IsUnique();
        });

        // === DbTrack ===
        modelBuilder.Entity<DbTrack>(e =>
        {
            e.HasIndex(t => t.Title);
            e.HasOne(t => t.AddedBy).WithMany().HasForeignKey(t => t.AddedByUserId).OnDelete(DeleteBehavior.SetNull);
        });

        // === UserTrackStats ===
        modelBuilder.Entity<UserTrackStats>(e =>
        {
            e.HasIndex(s => new { s.UserId, s.TrackId }).IsUnique();
            e.HasOne(s => s.User).WithMany().HasForeignKey(s => s.UserId);
            e.HasOne(s => s.Track).WithMany().HasForeignKey(s => s.TrackId);
        });

        // === Notification ===
        modelBuilder.Entity<Notification>(e =>
        {
            e.HasOne(n => n.User).WithMany().HasForeignKey(n => n.UserId);
            e.HasIndex(n => new { n.UserId, n.IsRead });
        });

        // === Playlist ===
        modelBuilder.Entity<Playlist>(e =>
        {
            e.HasOne(p => p.User).WithMany().HasForeignKey(p => p.UserId);
            e.HasIndex(p => p.UserId);
        });

        // === PlaylistTrack ===
        modelBuilder.Entity<PlaylistTrack>(e =>
        {
            e.HasOne(pt => pt.Playlist).WithMany(p => p.PlaylistTracks).HasForeignKey(pt => pt.PlaylistId);
            e.HasOne(pt => pt.Track).WithMany().HasForeignKey(pt => pt.TrackId);
            e.HasIndex(pt => new { pt.PlaylistId, pt.TrackId }).IsUnique();
        });

        // === ListeningHistory ===
        modelBuilder.Entity<ListeningHistory>(e =>
        {
            e.HasOne(lh => lh.User).WithMany().HasForeignKey(lh => lh.UserId);
            e.HasOne(lh => lh.Track).WithMany().HasForeignKey(lh => lh.TrackId);
            e.HasIndex(lh => new { lh.UserId, lh.StartedAt });
        });

        // === TrackComment ===
        modelBuilder.Entity<TrackComment>(e =>
        {
            e.HasOne(c => c.Track).WithMany().HasForeignKey(c => c.TrackId);
            e.HasOne(c => c.User).WithMany().HasForeignKey(c => c.UserId);
            e.HasIndex(c => c.TrackId);
            e.HasIndex(c => new { c.TrackId, c.CreatedAt });
        });

        // === CommentLike ===
        modelBuilder.Entity<CommentLike>(e =>
        {
            e.HasOne(cl => cl.Comment).WithMany(c => c.Likes).HasForeignKey(cl => cl.CommentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(cl => cl.User).WithMany().HasForeignKey(cl => cl.UserId);
            e.HasIndex(cl => new { cl.CommentId, cl.UserId }).IsUnique();
        });

        // === SongRequest ===
        modelBuilder.Entity<SongRequest>(e =>
        {
            e.HasOne(r => r.User).WithMany().HasForeignKey(r => r.UserId);
            e.HasOne(r => r.RespondedBy).WithMany().HasForeignKey(r => r.RespondedByUserId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(r => r.UserId);
            e.HasIndex(r => r.Status);
        });

        // === PrivateMessage ===
        modelBuilder.Entity<PrivateMessage>(e =>
        {
            e.HasOne(m => m.Sender).WithMany().HasForeignKey(m => m.SenderId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(m => m.Receiver).WithMany().HasForeignKey(m => m.ReceiverId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(m => new { m.SenderId, m.ReceiverId });
            e.HasIndex(m => m.SentAt);
        });

        // === SEED DATA ===

        // Admin user
        var adminHash = HashPassword("Admin123!");
        modelBuilder.Entity<AppUser>().HasData(new AppUser
        {
            Id = 1,
            Username = "admin",
            Email = "admin@musicly.com",
            PasswordHash = adminHash,
            Role = "Admin",
            IsActive = true,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        // Hacer user
        var hacerHash = HashPassword("Hacer123!");
        modelBuilder.Entity<AppUser>().HasData(new AppUser
        {
            Id = 2,
            Username = "hacer",
            Email = "hacer@musicly.com",
            PasswordHash = hacerHash,
            Role = "User",
            IsActive = true,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        // Seed tracks
        var seedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        modelBuilder.Entity<DbTrack>().HasData(
            new DbTrack { Id = 1, Title = "Ashes in Slow Motion", Artist = "Unknown Artist", FilePath = "music/Ashes in Slow Motion.mp3", Genre = "Cinematic", Mood = "Melancholy", GradientColor = "linear-gradient(135deg, #1a1a2e, #16213e, #0f3460, #e94560)", IconSvg = "<svg viewBox=\"0 0 24 24\" width=\"22\" height=\"22\" fill=\"rgba(255,255,255,0.8)\"><path d=\"M13.5.67s.74 2.65.74 4.8c0 2.06-1.35 3.73-3.41 3.73-2.07 0-3.63-1.67-3.63-3.73l.03-.36C5.21 7.51 4 10.62 4 14c0 4.42 3.58 8 8 8s8-3.58 8-8C20 8.61 17.41 3.8 13.5.67zM11.71 19c-1.78 0-3.22-1.4-3.22-3.14 0-1.62 1.05-2.76 2.81-3.12 1.77-.36 3.6-1.21 4.62-2.58.39 1.29.59 2.65.59 4.04 0 2.65-2.15 4.8-4.8 4.8z\"/></svg>", CreatedAt = seedDate, AddedByUserId = 1 },
            new DbTrack { Id = 2, Title = "Eclipsed Tides", Artist = "Unknown Artist", FilePath = "music/Eclipsed Tides.mp3", Genre = "Ambient", Mood = "Chill", GradientColor = "linear-gradient(135deg, #0c0c1d, #1b2845, #274060, #1b6ca8)", IconSvg = "<svg viewBox=\"0 0 24 24\" width=\"22\" height=\"22\" fill=\"rgba(255,255,255,0.8)\"><path d=\"M21 14c-1.15 0-2.1-.56-2.77-1.37C17.32 13.74 16.07 15 14.5 15c-1.57 0-2.82-1.26-3.23-2.37C10.6 13.44 9.65 14 8.5 14c-1.15 0-2.1-.56-2.77-1.37C4.82 13.74 3.57 15 2 15v2c1.57 0 2.82-1.26 3.23-2.37.67.81 1.62 1.37 2.77 1.37 1.15 0 2.1-.56 2.77-1.37.41 1.11 1.66 2.37 3.23 2.37 1.57 0 2.82-1.26 3.23-2.37.67.81 1.62 1.37 2.77 1.37V14z\"/></svg>", CreatedAt = seedDate, AddedByUserId = 1 },
            new DbTrack { Id = 3, Title = "High Octane Craic", Artist = "Unknown Artist", FilePath = "music/High Octane Craic.mp3", Genre = "Folk Rock", Mood = "Energetic", GradientColor = "linear-gradient(135deg, #2d1b00, #8b4513, #d2691e, #ff8c00)", IconSvg = "<svg viewBox=\"0 0 24 24\" width=\"22\" height=\"22\" fill=\"rgba(255,255,255,0.8)\"><path d=\"M12 3v10.55c-.59-.34-1.27-.55-2-.55-2.21 0-4 1.79-4 4s1.79 4 4 4 4-1.79 4-4V7h4V3h-6z\"/></svg>", CreatedAt = seedDate, AddedByUserId = 1 },
            new DbTrack { Id = 4, Title = "Blood in the Cobblestones", Artist = "Unknown Artist", FilePath = "music/Blood in the Cobblestones.mp3", Genre = "Dark Rock", Mood = "Intense", GradientColor = "linear-gradient(135deg, #1a0000, #4a0000, #8b0000, #cc0000)", IconSvg = "<svg viewBox=\"0 0 24 24\" width=\"22\" height=\"22\" fill=\"rgba(255,255,255,0.8)\"><path d=\"M12 2c-5.33 4.55-8 8.48-8 11.8 0 4.98 3.8 8.2 8 8.2s8-3.22 8-8.2c0-3.32-2.67-7.25-8-11.8z\"/></svg>", CreatedAt = seedDate, AddedByUserId = 1 },
            new DbTrack { Id = 5, Title = "Zerberus 145", Artist = "Unknown Artist", FilePath = "music/Zerberus 145.mp3", Genre = "Electronic", Mood = "Dark", GradientColor = "linear-gradient(135deg, #0d0d0d, #1a1a2e, #e94560, #533483)", IconSvg = "<svg viewBox=\"0 0 24 24\" width=\"22\" height=\"22\" fill=\"rgba(255,255,255,0.8)\"><path d=\"M7.5 5.6L10 7 8.6 4.5 10 2 7.5 3.4 5 2l1.4 2.5L5 7zm12 9.8L17 14l1.4 2.5L17 19l2.5-1.4L22 19l-1.4-2.5L22 14zM22 2l-2.5 1.4L17 2l1.4 2.5L17 7l2.5-1.4L22 7l-1.4-2.5zm-7.63 5.29a.996.996 0 0 0-1.41 0L1.29 18.96a.996.996 0 0 0 0 1.41l2.34 2.34c.39.39 1.02.39 1.41 0L16.7 11.05a.996.996 0 0 0 0-1.41l-2.33-2.35z\"/></svg>", CreatedAt = seedDate, AddedByUserId = 1 },
            new DbTrack { Id = 6, Title = "Sub Zero Velocity", Artist = "Unknown Artist", FilePath = "music/Sub_Zero_Velocity.mp3", Genre = "Synthwave", Mood = "Energetic", GradientColor = "linear-gradient(135deg, #001529, #003366, #00bfff, #e0f7ff)", IconSvg = "<svg viewBox=\"0 0 24 24\" width=\"22\" height=\"22\" fill=\"rgba(255,255,255,0.8)\"><path d=\"M22 11h-4.17l3.24-3.24-1.41-1.42L15 11h-2V9l4.66-4.66-1.42-1.41L13 6.17V2h-2v4.17L7.76 2.93 6.34 4.34 11 9v2H9L4.34 6.34 2.93 7.76 6.17 11H2v2h4.17l-3.24 3.24 1.41 1.42L9 13h2v2l-4.66 4.66 1.42 1.41L11 17.83V22h2v-4.17l3.24 3.24 1.42-1.41L13 15v-2h2l4.66 4.66 1.41-1.42L17.83 13H22z\"/></svg>", CreatedAt = seedDate, AddedByUserId = 1 },
            new DbTrack { Id = 7, Title = "Shockwave Runway", Artist = "Unknown Artist", FilePath = "music/Shockwave Runway.mp3", Genre = "EDM", Mood = "Energetic", GradientColor = "linear-gradient(135deg, #0a0a0a, #ff00ff, #00ffff, #ffff00)", IconSvg = "<svg viewBox=\"0 0 24 24\" width=\"22\" height=\"22\" fill=\"rgba(255,255,255,0.8)\"><path d=\"M7 2v11h3v9l7-12h-4l4-8z\"/></svg>", CreatedAt = seedDate, AddedByUserId = 1 },
            new DbTrack { Id = 8, Title = "Eclipsed Tides (Remix)", Artist = "Unknown Artist", FilePath = "music/Eclipsed Tides (1).mp3", Genre = "Ambient", Mood = "Chill", GradientColor = "linear-gradient(135deg, #0a0a2a, #1e3a5f, #2196f3, #64b5f6)", IconSvg = "<svg viewBox=\"0 0 24 24\" width=\"22\" height=\"22\" fill=\"rgba(255,255,255,0.8)\"><path d=\"M12 3a9 9 0 1 0 9 9c0-.46-.04-.92-.1-1.36a5.389 5.389 0 0 1-4.4 2.26 5.403 5.403 0 0 1-3.14-9.8c-.44-.06-.9-.1-1.36-.1z\"/></svg>", CreatedAt = seedDate, AddedByUserId = 1 },
            new DbTrack { Id = 9, Title = "Everyone Belongs to Hell", Artist = "Unknown Artist", FilePath = "music/Everyone Belongs to Hell.mp3", Genre = "Metal", Mood = "Intense", GradientColor = "linear-gradient(135deg, #0d0d0d, #2c003e, #950740, #c3073f)", IconSvg = "<svg viewBox=\"0 0 24 24\" width=\"22\" height=\"22\" fill=\"rgba(255,255,255,0.8)\"><path d=\"M11.5 9C10.12 9 9 10.12 9 11.5s1.12 2.5 2.5 2.5 2.5-1.12 2.5-2.5S12.88 9 11.5 9zM20 4H4c-1.1 0-2 .9-2 2v12c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V6c0-1.1-.9-2-2-2zm-3.21 14.21l-2.91-2.91c-.69.44-1.51.7-2.39.7C9.01 16 7 13.99 7 11.5S9.01 7 11.5 7 16 9.01 16 11.5c0 .88-.26 1.69-.7 2.39l2.91 2.91-1.42 1.41z\"/></svg>", CreatedAt = seedDate, AddedByUserId = 1 }
        );
    }

    public static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }
}
