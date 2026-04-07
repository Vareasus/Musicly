using Musicly.Data;
using Musicly.Models;
using Microsoft.EntityFrameworkCore;

namespace Musicly.Services;

public class PrivateMessageService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    // Allowed private message usernames (case-insensitive)
    private static readonly string[] AllowedUsernames = { "hacer", "vareasus" };

    public PrivateMessageService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    private bool IsAllowedUser(AppUser user)
    {
        return user.Role == "Admin" ||
               AllowedUsernames.Any(u => u.Equals(user.Username, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Checks if a user is allowed to use private messaging.
    /// </summary>
    public async Task<bool> CanAccessMessagesAsync(int userId)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var user = await db.Users.FindAsync(userId);
        if (user == null) return false;
        return IsAllowedUser(user);
    }

    /// <summary>
    /// Gets all chat partners for a given user.
    /// Admin gets all allowed users; allowed users get Admin.
    /// </summary>
    public async Task<List<(int PartnerId, string PartnerName)>> GetAllChatPartnersAsync(int currentUserId)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var currentUser = await db.Users.FindAsync(currentUserId);
        if (currentUser == null) return new();

        var partners = new List<(int, string)>();

        if (currentUser.Role == "Admin")
        {
            // Admin sees all allowed users
            var allowedUsers = await db.Users
                .Where(u => AllowedUsernames.Contains(u.Username.ToLower()))
                .ToListAsync();
            foreach (var u in allowedUsers)
                partners.Add((u.Id, u.Username));
        }
        else if (AllowedUsernames.Any(u => u.Equals(currentUser.Username, StringComparison.OrdinalIgnoreCase)))
        {
            // Allowed user sees Admin
            var admin = await db.Users.FirstOrDefaultAsync(u => u.Role == "Admin");
            if (admin != null)
                partners.Add((admin.Id, admin.Username));
        }

        return partners;
    }

    /// <summary>
    /// Gets the first chat partner (backward compatible).
    /// </summary>
    public async Task<(int PartnerId, string PartnerName)?> GetChatPartnerAsync(int currentUserId)
    {
        var partners = await GetAllChatPartnersAsync(currentUserId);
        if (partners.Count == 0) return null;
        return partners[0];
    }

    /// <summary>
    /// Sends a private message.
    /// </summary>
    public async Task<bool> SendMessageAsync(int senderId, int receiverId, string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return false;

        try
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            var sender = await db.Users.FindAsync(senderId);
            var receiver = await db.Users.FindAsync(receiverId);
            if (sender == null || receiver == null)
            {
                Console.WriteLine($"[MSG] sender={sender?.Username ?? "NULL"} receiver={receiver?.Username ?? "NULL"}");
                return false;
            }

            if (!IsAllowedUser(sender) || !IsAllowedUser(receiver))
            {
                Console.WriteLine($"[MSG] NOT ALLOWED: sender={sender.Username}({sender.Role}), receiver={receiver.Username}({receiver.Role})");
                return false;
            }

            var message = new PrivateMessage
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Content = content.Trim(),
                SentAt = DateTime.UtcNow,
                IsRead = false
            };

            db.PrivateMessages.Add(message);
            await db.SaveChangesAsync();
            Console.WriteLine($"[MSG] SUCCESS: {sender.Username} -> {receiver.Username}: {content}");
            return true;
        }
        catch (Exception ex)
        {
            var inner = ex.InnerException?.Message ?? "no inner";
            Console.WriteLine($"[MSG] EXCEPTION: {ex.Message} | INNER: {inner}");
            throw;
        }
    }

    /// <summary>
    /// Gets all messages between two users, ordered by time.
    /// </summary>
    public async Task<List<PrivateMessage>> GetConversationAsync(int userId1, int userId2)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        return await db.PrivateMessages
            .Include(m => m.Sender)
            .Include(m => m.Receiver)
            .Where(m => (m.SenderId == userId1 && m.ReceiverId == userId2)
                     || (m.SenderId == userId2 && m.ReceiverId == userId1))
            .OrderBy(m => m.SentAt)
            .ToListAsync();
    }

    /// <summary>
    /// Gets unread message count for a user.
    /// </summary>
    public async Task<int> GetUnreadCountAsync(int userId)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        return await db.PrivateMessages
            .CountAsync(m => m.ReceiverId == userId && !m.IsRead);
    }

    /// <summary>
    /// Marks all messages from otherUser to currentUser as read.
    /// </summary>
    public async Task MarkAsReadAsync(int currentUserId, int otherUserId)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var unread = await db.PrivateMessages
            .Where(m => m.SenderId == otherUserId && m.ReceiverId == currentUserId && !m.IsRead)
            .ToListAsync();

        foreach (var msg in unread)
        {
            msg.IsRead = true;
        }

        await db.SaveChangesAsync();
    }
}