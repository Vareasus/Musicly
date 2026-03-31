using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Musicly.Migrations
{
    /// <inheritdoc />
    public partial class AddHacerUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Bio", "CreatedAt", "Email", "IsActive", "LastLoginAt", "PasswordHash", "ProfileImageUrl", "Role", "Username" },
                values: new object[] { 2, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "hacer@musicly.com", true, null, "lnSQxP/Kxl6dK4NAFRgB8A75Z4d2fyRwDIzVVxWTlWI=", null, "User", "hacer" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2);
        }
    }
}
