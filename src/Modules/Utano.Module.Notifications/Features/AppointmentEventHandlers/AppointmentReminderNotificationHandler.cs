using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Utano.Module.Core.Domain.Events.Appointments;
using Utano.Module.Notifications.DatabaseMappings;
using Utano.Module.Notifications.Domain.Entities;
using Utano.Module.Notifications.Domain.Enums;
using Utano.Module.Notifications.Domain.Interfaces;

namespace Utano.Module.Notifications.Features.AppointmentEventHandlers;

// Fires from the reminder scan background job (no HTTP request, no real "actor" user) - unlike
// the other appointment handlers, there's no ICurrentUserService identity to attribute this to.
public class AppointmentReminderNotificationHandler(
    INotificationRepository repository,
    NotificationsDbContext db,
    ILogger<AppointmentReminderNotificationHandler> logger)
    : INotificationHandler<AppointmentReminderDueEvent>
{
    public async Task Handle(AppointmentReminderDueEvent domainEvent, CancellationToken cancellationToken)
    {
        try
        {
            var preference = await db.NotificationPreferences
                .IgnoreQueryFilters() // background job - no per-request practice context
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == domainEvent.DoctorId, cancellationToken);

            var inAppEnabled = preference?.InAppEnabled ?? true; // default when no row exists yet
            if (!inAppEnabled)
                return;

            var entity = Notification.Create(
                domainEvent.PracticeId,
                domainEvent.DoctorId,
                Guid.Empty,
                "System",
                "Upcoming appointment reminder",
                $"{domainEvent.PatientName} on {domainEvent.AppointmentDate:d MMM yyyy} at {domainEvent.StartTime:h:mm tt}.",
                NotificationType.AppointmentReminder,
                domainEvent.AppointmentId);

            await repository.AddAsync(entity, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to create reminder notification for appointment {AppointmentId}",
                domainEvent.AppointmentId);
        }
    }
}
