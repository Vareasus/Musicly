using Musicly.Data;
using Musicly.Models;
using Microsoft.EntityFrameworkCore;

namespace Musicly.Services;

public class PrivateMessageService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public PrivateMessageService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    /// <summary>
    /// Checks if a user is allowed to use private messaging (must be "hacer" username or Admin role).
    /// </summary>
    public async Task<bool> CanAccessMessagesAsync(int userId)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var user = await db.Users.FindAsync(userId);
        if (user == null) return false;
        return user.Role == "Admin" || user.Username.Equals("hacer", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the chat partner ID for a given user (hacer gets admin, admin gets hacer).
    /// </summary>
    public async Task<(int PartnerId, string PartnerName)?> GetChatPartnerAsync(int currentUserId)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var currentUser = await db.Users.FindAsync(currentUserId);
        if (currentUser == null) return null;

        AppUser? partner = null;

        if (currentUser.Role == "Admin")
        {
            // Admin's chat partner is "hacer"
            partner = await db.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == "hacer");
        }
        else if (currentUser.Username.Equals("hacer", StringComparison.OrdinalIgnoreCase))
        {
            // Hacer's chat partner is the admin
            partner = await db.Users.FirstOrDefaultAsync(u => u.Role == "Admin");
        }

        if (partner == null) return null;
        return (partner.Id, partner.Username);
    }

    /// <summary>
    /// Sends a private message.
    /// </summary>
    public async Task<bool> SendMessageAsync(int senderId, int receiverId, string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return false;

        // Verify both users are allowed
        using var db = await _dbFactory.CreateDbContextAsync();
        var sender = await db.Users.FindAsync(senderId);
        var receiver = await db.Users.FindAsync(receiverId);
        if (sender == null || receiver == null) return false;

        bool senderAllowed = sender.Role == "Admin" || sender.Username.Equals("hacer", StringComparison.OrdinalIgnoreCase);
        bool receiverAllowed = receiver.Role == "Admin" || receiver.Username.Equals("hacer", StringComparison.OrdinalIgnoreCase);
        if (!senderAllowed || !receiverAllowed) return false;

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
        return true;
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
