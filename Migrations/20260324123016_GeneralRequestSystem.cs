using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AycaMusic.Migrations
{
    /// <inheritdoc />
    public partial class GeneralRequestSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArtistName",
                table: "SongRequests");

            migrationBuilder.RenameColumn(
                name: "SongTitle",
                table: "SongRequests",
                newName: "Title");

            migrationBuilder.RenameColumn(
                name: "Message",
                table: "SongRequests",
                newName: "Description");

            migrationBuilder.AddColumn<int>(
                name: "Category",
                table: "SongRequests",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "SongRequests");

            migrationBuilder.RenameColumn(
                name: "Title",
                table: "SongRequests",
                newName: "SongTitle");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "SongRequests",
                newName: "Message");

            migrationBuilder.AddColumn<string>(
                name: "ArtistName",
                table: "SongRequests",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
