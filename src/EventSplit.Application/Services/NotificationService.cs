using EventSplit.Application.DTOs;
using EventSplit.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EventSplit.Application.Services;

public class NotificationService
{
    private readonly IAppDbContext _db;

    public NotificationService(IAppDbContext db) => _db = db;

    public async Task<List<NotificationResponse>> GetMyNotificationsAsync(Guid userId, int limit = 20)
    {
        return await _db.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(limit)
            .Select(n => new NotificationResponse(
                n.Id, n.Message, n.Type, n.EventId, n.IsRead, n.CreatedAt
            ))
            .ToListAsync();
    }

    public async Task<int> GetUnreadCountAsync(Guid userId)
    {
        return await _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .CountAsync();
    }

    public async Task MarkAsReadAsync(Guid notificationId, Guid userId)
    {
        var notification = await _db.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

        if (notification != null)
        {
            notification.IsRead = true;
            await _db.SaveChangesAsync();
        }
    }

    public async Task MarkAllAsReadAsync(Guid userId)
    {
        var unread = await _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        foreach (var n in unread)
            n.IsRead = true;

        if (unread.Count > 0)
            await _db.SaveChangesAsync();
    }
}
