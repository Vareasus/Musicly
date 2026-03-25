using AycaMusic.Data;
using AycaMusic.Models;
using Microsoft.EntityFrameworkCore;

namespace AycaMusic.Services;

public class AuthService
{
    private readonly AppDbContext _db;

    public AuthService(AppDbContext db)
    {
        _db = db;
    }

    // Password validation rules
    public static (bool IsValid, List<string> Errors) ValidatePassword(string password)
    {
        var errors = new List<string>();
        if (password.Length < 8)
            errors.Add("En az 8 karakter olmalı");
        if (!password.Any(char.IsUpper))
            errors.Add("En az 1 büyük harf olmalı");
        if (!password.Any(char.IsLower))
            errors.Add("En az 1 küçük harf olmalı");
        if (!password.Any(char.IsDigit))
            errors.Add("En az 1 rakam olmalı");
        if (!password.Any(c => "!@#$%^&*()_+-=[]{}|;':\",./<>?".Contains(c)))
            errors.Add("En az 1 özel karakter olmalı (!@#$%... vb.)");
        return (errors.Count == 0, errors);
    }

    public async Task<(bool Success, string Error)> RegisterAsync(string username, string email, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return (false, "Tüm alanlar zorunludur");

        var (isValid, errors) = ValidatePassword(password);
        if (!isValid)
            return (false, string.Join(", ", errors));

        if (await _db.Users.AnyAsync(u => u.Username.ToLower() == username.ToLower()))
            return (false, "Bu kullanıcı adı zaten kullanılıyor");

        if (await _db.Users.AnyAsync(u => u.Email.ToLower() == email.ToLower()))
            return (false, "Bu e-posta zaten kullanılıyor");

        var user = new AppUser
        {
            Username = username.Trim(),
            Email = email.Trim().ToLower(),
            PasswordHash = AppDbContext.HashPassword(password),
            Role = "User",
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return (true, "");
    }

    public async Task<AppUser?> LoginAsync(string username, string password)
    {
        var hash = AppDbContext.HashPassword(password);
        var user = await _db.Users.FirstOrDefaultAsync(u =>
            u.Username.ToLower() == username.ToLower() && u.PasswordHash == hash);
        if (user != null)
        {
            user.LastLoginAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
        return user;
    }

    public async Task<AppUser?> GetUserAsync(int userId)
    {
        return await _db.Users.FindAsync(userId);
    }

    public async Task<List<AppUser>> GetAllUsersAsync()
    {
        return await _db.Users.OrderByDescending(u => u.CreatedAt).ToListAsync();
    }
}
