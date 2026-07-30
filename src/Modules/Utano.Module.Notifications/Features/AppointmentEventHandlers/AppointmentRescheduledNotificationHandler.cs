using MediatR;
using Microsoft.Extensions.Logging;
using Utano.Module.Core.Domain.Events.Appointments;
using Utano.Module.Core.Services;
using Utano.Module.Notifications.Domain.Entities;
using Utano.Module.Notifications.Domain.Enums;
using Utano.Module.Notifications.Domain.Interfaces;

namespace Utano.Module.Notifications.Features.AppointmentEventHandlers;

public class AppointmentRescheduledNotificationHandler(
    INotificationRepository repository,
    ICurrentUserService currentUserService,
    ILogger<AppointmentRescheduledNotificationHandler> logger)
    : INotificationHandler<AppointmentRescheduledEvent>
{
    public async Task Handle(AppointmentRescheduledEvent domainEvent, CancellationToken cancellationToken)
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
                "Appointment rescheduled",
                $"{domainEvent.PatientName}'s appointment moved to {domainEvent.NewDate:d MMM yyyy} at {domainEvent.NewStartTime:h:mm tt}.",
                NotificationType.AppointmentRescheduled,
                domainEvent.AppointmentId);

            await repository.AddAsync(entity, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to create rescheduled-appointment notification for appointment {AppointmentId}",
                domainEvent.AppointmentId);
        }
    }
}
