namespace Musicly.Models;

public class AppUser
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "User"; // "Admin" or "User"
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Professional fields
    public string? ProfileImageUrl { get; set; }
    public string? Bio { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; } = true;
}
