using MediatR;
using Microsoft.Extensions.Logging;
using Utano.Module.Core.Domain.Events.Appointments;
using Utano.Module.Core.Services;
using Utano.Module.Notifications.Domain.Entities;
using Utano.Module.Notifications.Domain.Enums;
using Utano.Module.Notifications.Domain.Interfaces;

namespace Utano.Module.Notifications.Features.AppointmentEventHandlers;

public class AppointmentCancelledNotificationHandler(
    INotificationRepository repository,
    ICurrentUserService currentUserService,
    ILogger<AppointmentCancelledNotificationHandler> logger)
    : INotificationHandler<AppointmentCancelledEvent>
{
    public async Task Handle(AppointmentCancelledEvent domainEvent, CancellationToken cancellationToken)
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
                "Appointment cancelled",
                $"{domainEvent.PatientName}'s appointment on {domainEvent.AppointmentDate:d MMM yyyy} at {domainEvent.StartTime:h:mm tt} was cancelled. Reason: {domainEvent.Reason}",
                NotificationType.AppointmentCancelled,
                domainEvent.AppointmentId);

            await repository.AddAsync(entity, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to create cancelled-appointment notification for appointment {AppointmentId}",
                domainEvent.AppointmentId);
        }
    }
}
