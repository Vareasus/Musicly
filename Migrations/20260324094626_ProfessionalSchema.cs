using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AycaMusic.Migrations
{
    /// <inheritdoc />
    public partial class ProfessionalSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Username = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProfileImageUrl = table.Column<string>(type: "text", nullable: true),
                    Bio = table.Column<string>(type: "text", nullable: true),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Playlists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Playlists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Playlists_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Tracks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Artist = table.Column<string>(type: "text", nullable: false),
                    FilePath = table.Column<string>(type: "text", nullable: false),
                    Genre = table.Column<string>(type: "text", nullable: false),
                    Mood = table.Column<string>(type: "text", nullable: false),
                    GradientColor = table.Column<string>(type: "text", nullable: false),
                    IconSvg = table.Column<string>(type: "text", nullable: false),
                    DurationSeconds = table.Column<double>(type: "double precision", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AddedByUserId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tracks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tracks_Users_AddedByUserId",
                        column: x => x.AddedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ListeningHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    TrackId = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DurationSeconds = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListeningHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ListeningHistory_Tracks_TrackId",
                        column: x => x.TrackId,
                        principalTable: "Tracks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ListeningHistory_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlaylistTracks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlaylistId = table.Column<int>(type: "integer", nullable: false),
                    TrackId = table.Column<int>(type: "integer", nullable: false),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaylistTracks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlaylistTracks_Playlists_PlaylistId",
                        column: x => x.PlaylistId,
                        principalTable: "Playlists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlaylistTracks_Tracks_TrackId",
                        column: x => x.TrackId,
                        principalTable: "Tracks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserTrackStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    TrackId = table.Column<int>(type: "integer", nullable: false),
                    PlayCount = table.Column<int>(type: "integer", nullable: false),
                    TotalListeningSeconds = table.Column<double>(type: "double precision", nullable: false),
                    LastPlayed = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsLiked = table.Column<bool>(type: "boolean", nullable: false),
                    IsDisliked = table.Column<bool>(type: "boolean", nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    FirstPlayedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTrackStats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserTrackStats_Tracks_TrackId",
                        column: x => x.TrackId,
                        principalTable: "Tracks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserTrackStats_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Bio", "CreatedAt", "Email", "IsActive", "LastLoginAt", "PasswordHash", "ProfileImageUrl", "Role", "Username" },
                values: new object[] { 1, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "admin@musicly.com", true, null, "PrP+ZrMeO00Q+nC1ytSccRIpSvauTkdqHEBRVdRaoSE=", null, "Admin", "admin" });

            migrationBuilder.InsertData(
                table: "Tracks",
                columns: new[] { "Id", "AddedByUserId", "Artist", "CreatedAt", "DurationSeconds", "FilePath", "Genre", "GradientColor", "IconSvg", "Mood", "Title" },
                values: new object[,]
                {
                    { 1, 1, "Unknown Artist", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.0, "music/Ashes in Slow Motion.mp3", "Cinematic", "linear-gradient(135deg, #1a1a2e, #16213e, #0f3460, #e94560)", "<svg viewBox=\"0 0 24 24\" width=\"22\" height=\"22\" fill=\"rgba(255,255,255,0.8)\"><path d=\"M13.5.67s.74 2.65.74 4.8c0 2.06-1.35 3.73-3.41 3.73-2.07 0-3.63-1.67-3.63-3.73l.03-.36C5.21 7.51 4 10.62 4 14c0 4.42 3.58 8 8 8s8-3.58 8-8C20 8.61 17.41 3.8 13.5.67zM11.71 19c-1.78 0-3.22-1.4-3.22-3.14 0-1.62 1.05-2.76 2.81-3.12 1.77-.36 3.6-1.21 4.62-2.58.39 1.29.59 2.65.59 4.04 0 2.65-2.15 4.8-4.8 4.8z\"/></svg>", "Melancholy", "Ashes in Slow Motion" },
                    { 2, 1, "Unknown Artist", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.0, "music/Eclipsed Tides.mp3", "Ambient", "linear-gradient(135deg, #0c0c1d, #1b2845, #274060, #1b6ca8)", "<svg viewBox=\"0 0 24 24\" width=\"22\" height=\"22\" fill=\"rgba(255,255,255,0.8)\"><path d=\"M21 14c-1.15 0-2.1-.56-2.77-1.37C17.32 13.74 16.07 15 14.5 15c-1.57 0-2.82-1.26-3.23-2.37C10.6 13.44 9.65 14 8.5 14c-1.15 0-2.1-.56-2.77-1.37C4.82 13.74 3.57 15 2 15v2c1.57 0 2.82-1.26 3.23-2.37.67.81 1.62 1.37 2.77 1.37 1.15 0 2.1-.56 2.77-1.37.41 1.11 1.66 2.37 3.23 2.37 1.57 0 2.82-1.26 3.23-2.37.67.81 1.62 1.37 2.77 1.37V14z\"/></svg>", "Chill", "Eclipsed Tides" },
                    { 3, 1, "Unknown Artist", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.0, "music/High Octane Craic.mp3", "Folk Rock", "linear-gradient(135deg, #2d1b00, #8b4513, #d2691e, #ff8c00)", "<svg viewBox=\"0 0 24 24\" width=\"22\" height=\"22\" fill=\"rgba(255,255,255,0.8)\"><path d=\"M12 3v10.55c-.59-.34-1.27-.55-2-.55-2.21 0-4 1.79-4 4s1.79 4 4 4 4-1.79 4-4V7h4V3h-6z\"/></svg>", "Energetic", "High Octane Craic" },
                    { 4, 1, "Unknown Artist", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.0, "music/Blood in the Cobblestones.mp3", "Dark Rock", "linear-gradient(135deg, #1a0000, #4a0000, #8b0000, #cc0000)", "<svg viewBox=\"0 0 24 24\" width=\"22\" height=\"22\" fill=\"rgba(255,255,255,0.8)\"><path d=\"M12 2c-5.33 4.55-8 8.48-8 11.8 0 4.98 3.8 8.2 8 8.2s8-3.22 8-8.2c0-3.32-2.67-7.25-8-11.8z\"/></svg>", "Intense", "Blood in the Cobblestones" },
                    { 5, 1, "Unknown Artist", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.0, "music/Zerberus 145.mp3", "Electronic", "linear-gradient(135deg, #0d0d0d, #1a1a2e, #e94560, #533483)", "<svg viewBox=\"0 0 24 24\" width=\"22\" height=\"22\" fill=\"rgba(255,255,255,0.8)\"><path d=\"M7.5 5.6L10 7 8.6 4.5 10 2 7.5 3.4 5 2l1.4 2.5L5 7zm12 9.8L17 14l1.4 2.5L17 19l2.5-1.4L22 19l-1.4-2.5L22 14zM22 2l-2.5 1.4L17 2l1.4 2.5L17 7l2.5-1.4L22 7l-1.4-2.5zm-7.63 5.29a.996.996 0 0 0-1.41 0L1.29 18.96a.996.996 0 0 0 0 1.41l2.34 2.34c.39.39 1.02.39 1.41 0L16.7 11.05a.996.996 0 0 0 0-1.41l-2.33-2.35z\"/></svg>", "Dark", "Zerberus 145" },
                    { 6, 1, "Unknown Artist", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.0, "music/Sub_Zero_Velocity.mp3", "Synthwave", "linear-gradient(135deg, #001529, #003366, #00bfff, #e0f7ff)", "<svg viewBox=\"0 0 24 24\" width=\"22\" height=\"22\" fill=\"rgba(255,255,255,0.8)\"><path d=\"M22 11h-4.17l3.24-3.24-1.41-1.42L15 11h-2V9l4.66-4.66-1.42-1.41L13 6.17V2h-2v4.17L7.76 2.93 6.34 4.34 11 9v2H9L4.34 6.34 2.93 7.76 6.17 11H2v2h4.17l-3.24 3.24 1.41 1.42L9 13h2v2l-4.66 4.66 1.42 1.41L11 17.83V22h2v-4.17l3.24 3.24 1.42-1.41L13 15v-2h2l4.66 4.66 1.41-1.42L17.83 13H22z\"/></svg>", "Energetic", "Sub Zero Velocity" },
                    { 7, 1, "Unknown Artist", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.0, "music/Shockwave Runway.mp3", "EDM", "linear-gradient(135deg, #0a0a0a, #ff00ff, #00ffff, #ffff00)", "<svg viewBox=\"0 0 24 24\" width=\"22\" height=\"22\" fill=\"rgba(255,255,255,0.8)\"><path d=\"M7 2v11h3v9l7-12h-4l4-8z\"/></svg>", "Energetic", "Shockwave Runway" },
                    { 8, 1, "Unknown Artist", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.0, "music/Eclipsed Tides (1).mp3", "Ambient", "linear-gradient(135deg, #0a0a2a, #1e3a5f, #2196f3, #64b5f6)", "<svg viewBox=\"0 0 24 24\" width=\"22\" height=\"22\" fill=\"rgba(255,255,255,0.8)\"><path d=\"M12 3a9 9 0 1 0 9 9c0-.46-.04-.92-.1-1.36a5.389 5.389 0 0 1-4.4 2.26 5.403 5.403 0 0 1-3.14-9.8c-.44-.06-.9-.1-1.36-.1z\"/></svg>", "Chill", "Eclipsed Tides (Remix)" },
                    { 9, 1, "Unknown Artist", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.0, "music/Everyone Belongs to Hell.mp3", "Metal", "linear-gradient(135deg, #0d0d0d, #2c003e, #950740, #c3073f)", "<svg viewBox=\"0 0 24 24\" width=\"22\" height=\"22\" fill=\"rgba(255,255,255,0.8)\"><path d=\"M11.5 9C10.12 9 9 10.12 9 11.5s1.12 2.5 2.5 2.5 2.5-1.12 2.5-2.5S12.88 9 11.5 9zM20 4H4c-1.1 0-2 .9-2 2v12c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V6c0-1.1-.9-2-2-2zm-3.21 14.21l-2.91-2.91c-.69.44-1.51.7-2.39.7C9.01 16 7 13.99 7 11.5S9.01 7 11.5 7 16 9.01 16 11.5c0 .88-.26 1.69-.7 2.39l2.91 2.91-1.42 1.41z\"/></svg>", "Intense", "Everyone Belongs to Hell" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ListeningHistory_TrackId",
                table: "ListeningHistory",
                column: "TrackId");

            migrationBuilder.CreateIndex(
                name: "IX_ListeningHistory_UserId_StartedAt",
                table: "ListeningHistory",
                columns: new[] { "UserId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_Playlists_UserId",
                table: "Playlists",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PlaylistTracks_PlaylistId_TrackId",
                table: "PlaylistTracks",
                columns: new[] { "PlaylistId", "TrackId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlaylistTracks_TrackId",
                table: "PlaylistTracks",
                column: "TrackId");

            migrationBuilder.CreateIndex(
                name: "IX_Tracks_AddedByUserId",
                table: "Tracks",
                column: "AddedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Tracks_Title",
                table: "Tracks",
                column: "Title");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserTrackStats_TrackId",
                table: "UserTrackStats",
                column: "TrackId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTrackStats_UserId_TrackId",
                table: "UserTrackStats",
                columns: new[] { "UserId", "TrackId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ListeningHistory");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "PlaylistTracks");

            migrationBuilder.DropTable(
                name: "UserTrackStats");

            migrationBuilder.DropTable(
                name: "Playlists");

            migrationBuilder.DropTable(
                name: "Tracks");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
