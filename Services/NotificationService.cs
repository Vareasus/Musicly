using AycaMusic.Data;
using AycaMusic.Models;
using Microsoft.EntityFrameworkCore;

namespace AycaMusic.Services;

public class NotificationService
{
    private readonly AppDbContext _db;
    public event Action? OnNotificationsChanged;

    public NotificationService(AppDbContext db)
    {
        _db = db;
    }

    public async Task NotifyAllUsersAsync(string message)
    {
        var userIds = await _db.Users.Where(u => u.Role != "Admin").Select(u => u.Id).ToListAsync();
        foreach (var userId in userIds)
        {
            _db.Notifications.Add(new Notification
            {
                UserId = userId,
                Message = message,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            });
        }
        await _db.SaveChangesAsync();
        OnNotificationsChanged?.Invoke();
    }

    public async Task NotifyUserAsync(int userId, string message)
    {
        _db.Notifications.Add(new Notification
        {
            UserId = userId,
            Message = message,
            CreatedAt = DateTime.UtcNow,
            IsRead = false
        });
        await _db.SaveChangesAsync();
        OnNotificationsChanged?.Invoke();
    }

    public async Task<List<Notification>> GetUserNotificationsAsync(int userId, int count = 20)
    {
        return await _db.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        return await _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);
    }

    public async Task MarkAsReadAsync(int notificationId)
    {
        var notif = await _db.Notifications.FindAsync(notificationId);
        if (notif != null)
        {
            notif.IsRead = true;
            await _db.SaveChangesAsync();
        }
    }

    public async Task MarkAllAsReadAsync(int userId)
    {
        var unread = await _db.Notifications.Where(n => n.UserId == userId && !n.IsRead).ToListAsync();
        foreach (var n in unread) n.IsRead = true;
        await _db.SaveChangesAsync();
    }
}
