using MediatR;
using Microsoft.Extensions.Logging;
using Utano.Module.Core.Domain.Events.Appointments;
using Utano.Module.Core.Services;
using Utano.Module.Notifications.Domain.Entities;
using Utano.Module.Notifications.Domain.Enums;
using Utano.Module.Notifications.Domain.Interfaces;

namespace Utano.Module.Notifications.Features.AppointmentEventHandlers;

// A failure in here must never surface as an error on the originating request - the
// appointment write already committed by the time this runs (interceptor fires after
// SaveChanges), so a notification failure is logged and swallowed, not rethrown.
public class AppointmentBookedNotificationHandler(
    INotificationRepository repository,
    ICurrentUserService currentUserService,
    ILogger<AppointmentBookedNotificationHandler> logger)
    : INotificationHandler<AppointmentBookedEvent>
{
    public async Task Handle(AppointmentBookedEvent domainEvent, CancellationToken cancellationToken)
    {
        if (domainEvent.DoctorId == currentUserService.UserId)
            return;

        try
        {
            var entity = Notification.Create(
                domainEvent.PracticeId,
                domainEvent.DoctorId,
                currentUserService.UserId,
                currentUserService.FullName,
                "New appointment booked",
                $"{domainEvent.PatientName} booked for {domainEvent.AppointmentDate:d MMM yyyy} at {domainEvent.StartTime:h:mm tt}.",
                NotificationType.AppointmentBooked,
                domainEvent.AppointmentId);

            await repository.AddAsync(entity, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to create booked-appointment notification for appointment {AppointmentId}",
                domainEvent.AppointmentId);
        }
    }
}
