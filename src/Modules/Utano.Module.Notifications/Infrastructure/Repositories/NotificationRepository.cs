using Microsoft.EntityFrameworkCore;
using Utano.Module.Core.Services;
using Utano.Module.Notifications.DatabaseMappings;
using Utano.Module.Notifications.Domain.Entities;
using Utano.Module.Notifications.Domain.Interfaces;

namespace Utano.Module.Notifications.Infrastructure.Repositories;

public class NotificationRepository(
    NotificationsDbContext context,
    ICurrentUserService currentUserService) : INotificationRepository
{
    public async Task AddAsync(Notification notification, CancellationToken cancellationToken = default)
        => await context.Notifications.AddAsync(notification, cancellationToken);

    public async Task<IReadOnlyList<Notification>> GetMyNotificationsAsync(
        int limit = 30, CancellationToken cancellationToken = default)
        => await context.Notifications
            .Where(n => n.RecipientUserId == currentUserService.UserId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(limit)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<int> GetUnreadCountAsync(CancellationToken cancellationToken = default)
        => await context.Notifications
            .Where(n => n.RecipientUserId == currentUserService.UserId && !n.IsRead)
            .CountAsync(cancellationToken);

    public async Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.RecipientUserId == currentUserService.UserId, cancellationToken);

    public async Task MarkAllAsReadAsync(CancellationToken cancellationToken = default)
        => await context.Notifications
            .Where(n => n.RecipientUserId == currentUserService.UserId && !n.IsRead)
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.UpdatedAt, DateTimeOffset.UtcNow),
            cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => await context.SaveChangesAsync(cancellationToken);
}
