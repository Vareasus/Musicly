using Musicly.Data;
using Musicly.Models;
using Microsoft.EntityFrameworkCore;

namespace Musicly.Services;

public class SongRequestService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly NotificationService _notifService;

    public SongRequestService(IDbContextFactory<AppDbContext> dbFactory, NotificationService notifService)
    {
        _dbFactory = dbFactory;
        _notifService = notifService;
    }

    /// <summary>Submit a new general request</summary>
    public async Task<SongRequest> SubmitRequestAsync(int userId, string username, RequestCategory category, string title, string? description)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var request = new SongRequest
        {
            UserId = userId,
            Username = username,
            Category = category,
            Title = title.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            Status = RequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        db.SongRequests.Add(request);
        await db.SaveChangesAsync();
        return request;
    }

    /// <summary>Get all requests for a specific user</summary>
    public async Task<List<SongRequest>> GetUserRequestsAsync(int userId)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        return await db.SongRequests
            .AsNoTracking()
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    /// <summary>Get all requests (admin)</summary>
    public async Task<List<SongRequest>> GetAllRequestsAsync()
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        return await db.SongRequests
            .AsNoTracking()
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    /// <summary>Get pending request count (admin)</summary>
    public async Task<int> GetPendingCountAsync()
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        return await db.SongRequests.CountAsync(r => r.Status == RequestStatus.Pending);
    }

    /// <summary>Approve or reject a request with optional message</summary>
    public async Task RespondToRequestAsync(int requestId, RequestStatus status, string? adminResponse, int adminUserId)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var request = await db.SongRequests.FindAsync(requestId);
        if (request == null) return;

        request.Status = status;
        request.AdminResponse = string.IsNullOrWhiteSpace(adminResponse) ? null : adminResponse.Trim();
        request.RespondedByUserId = adminUserId;
        request.RespondedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        // Notify the user
        var emoji = status == RequestStatus.Approved ? "✅" : "❌";
        var statusText = status == RequestStatus.Approved ? "onaylandı" : "reddedildi";
        var notifMsg = $"{emoji} Talebin \"{request.Title}\" {statusText}!";
        if (!string.IsNullOrEmpty(request.AdminResponse))
        {
            notifMsg += $" — \"{request.AdminResponse}\"";
        }
        await _notifService.NotifyUserAsync(request.UserId, notifMsg);
    }

    /// <summary>Delete a request (admin)</summary>
    public async Task DeleteRequestAsync(int requestId)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var request = await db.SongRequests.FindAsync(requestId);
        if (request != null)
        {
            db.SongRequests.Remove(request);
            await db.SaveChangesAsync();
        }
    }
}
