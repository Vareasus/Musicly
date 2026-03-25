using AycaMusic.Data;
using AycaMusic.Models;
using AycaMusic.Services;
using Microsoft.EntityFrameworkCore;

namespace AycaMusic.Tests.Services;

public class NotificationServiceTests
{
    private AppDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    [Fact]
    public async Task NotifyUserAsync_ShouldAddNotification()
    {
        var db = CreateInMemoryDb();
        var user = new AppUser { Username = "test", Email = "t@t.com", PasswordHash = "x" };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var service = new NotificationService(db);
        await service.NotifyUserAsync(user.Id, "Hello!");

        var notifs = await db.Notifications.ToListAsync();
        Assert.Single(notifs);
        Assert.Equal("Hello!", notifs[0].Message);
        Assert.Equal(user.Id, notifs[0].UserId);
        Assert.False(notifs[0].IsRead);
    }

    [Fact]
    public async Task NotifyAllUsersAsync_ShouldNotifyNonAdminUsers()
    {
        var db = CreateInMemoryDb();
        db.Users.Add(new AppUser { Username = "user1", Email = "u1@t.com", PasswordHash = "x", Role = "User" });
        db.Users.Add(new AppUser { Username = "user2", Email = "u2@t.com", PasswordHash = "x", Role = "User" });
        db.Users.Add(new AppUser { Username = "admin1", Email = "a@t.com", PasswordHash = "x", Role = "Admin" });
        await db.SaveChangesAsync();

        var service = new NotificationService(db);
        await service.NotifyAllUsersAsync("Broadcast!");

        var notifs = await db.Notifications.ToListAsync();
        Assert.Equal(2, notifs.Count);
        Assert.All(notifs, n => Assert.Equal("Broadcast!", n.Message));
    }

    [Fact]
    public async Task GetUserNotificationsAsync_ShouldReturnOnlyUserNotifications()
    {
        var db = CreateInMemoryDb();
        db.Notifications.Add(new Notification { UserId = 1, Message = "For User 1", CreatedAt = DateTime.UtcNow });
        db.Notifications.Add(new Notification { UserId = 2, Message = "For User 2", CreatedAt = DateTime.UtcNow });
        db.Notifications.Add(new Notification { UserId = 1, Message = "Also for User 1", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var service = new NotificationService(db);
        var result = await service.GetUserNotificationsAsync(1);

        Assert.Equal(2, result.Count);
        Assert.All(result, n => Assert.Equal(1, n.UserId));
    }

    [Fact]
    public async Task GetUserNotificationsAsync_ShouldRespectCountLimit()
    {
        var db = CreateInMemoryDb();
        for (int i = 0; i < 10; i++)
            db.Notifications.Add(new Notification { UserId = 1, Message = $"Msg {i}", CreatedAt = DateTime.UtcNow.AddMinutes(i) });
        await db.SaveChangesAsync();

        var service = new NotificationService(db);
        var result = await service.GetUserNotificationsAsync(1, count: 3);

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task GetUserNotificationsAsync_ShouldOrderByNewest()
    {
        var db = CreateInMemoryDb();
        db.Notifications.Add(new Notification { UserId = 1, Message = "Old", CreatedAt = DateTime.UtcNow.AddHours(-2) });
        db.Notifications.Add(new Notification { UserId = 1, Message = "New", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var service = new NotificationService(db);
        var result = await service.GetUserNotificationsAsync(1);

        Assert.Equal("New", result[0].Message);
        Assert.Equal("Old", result[1].Message);
    }

    [Fact]
    public async Task GetUnreadCountAsync_ShouldReturnCorrectCount()
    {
        var db = CreateInMemoryDb();
        db.Notifications.Add(new Notification { UserId = 1, Message = "Unread", IsRead = false });
        db.Notifications.Add(new Notification { UserId = 1, Message = "Read", IsRead = true });
        db.Notifications.Add(new Notification { UserId = 1, Message = "Unread2", IsRead = false });
        db.Notifications.Add(new Notification { UserId = 2, Message = "Other", IsRead = false });
        await db.SaveChangesAsync();

        var service = new NotificationService(db);
        var count = await service.GetUnreadCountAsync(1);

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task MarkAsReadAsync_ShouldMarkSingleNotification()
    {
        var db = CreateInMemoryDb();
        var notif = new Notification { UserId = 1, Message = "Test", IsRead = false };
        db.Notifications.Add(notif);
        await db.SaveChangesAsync();

        var service = new NotificationService(db);
        await service.MarkAsReadAsync(notif.Id);

        var updated = await db.Notifications.FindAsync(notif.Id);
        Assert.True(updated!.IsRead);
    }

    [Fact]
    public async Task MarkAsReadAsync_NonExistentId_ShouldNotThrow()
    {
        var db = CreateInMemoryDb();
        var service = new NotificationService(db);
        await service.MarkAsReadAsync(9999);
        // Should not throw
    }

    [Fact]
    public async Task MarkAllAsReadAsync_ShouldMarkAllForUser()
    {
        var db = CreateInMemoryDb();
        db.Notifications.Add(new Notification { UserId = 1, Message = "A", IsRead = false });
        db.Notifications.Add(new Notification { UserId = 1, Message = "B", IsRead = false });
        db.Notifications.Add(new Notification { UserId = 2, Message = "C", IsRead = false });
        await db.SaveChangesAsync();

        var service = new NotificationService(db);
        await service.MarkAllAsReadAsync(1);

        var user1 = await db.Notifications.Where(n => n.UserId == 1).ToListAsync();
        Assert.All(user1, n => Assert.True(n.IsRead));

        var user2 = await db.Notifications.Where(n => n.UserId == 2).ToListAsync();
        Assert.All(user2, n => Assert.False(n.IsRead));
    }

    [Fact]
    public async Task OnNotificationsChanged_ShouldFireWhenNotifying()
    {
        var db = CreateInMemoryDb();
        db.Users.Add(new AppUser { Username = "test", Email = "t@t.com", PasswordHash = "x", Role = "User" });
        await db.SaveChangesAsync();

        var service = new NotificationService(db);
        bool fired = false;
        service.OnNotificationsChanged += () => fired = true;

        await service.NotifyUserAsync(1, "Test");

        Assert.True(fired);
    }

    [Fact]
    public async Task GetUnreadCountAsync_NoNotifications_ShouldReturnZero()
    {
        var db = CreateInMemoryDb();
        var service = new NotificationService(db);
        var count = await service.GetUnreadCountAsync(999);
        Assert.Equal(0, count);
    }
}
