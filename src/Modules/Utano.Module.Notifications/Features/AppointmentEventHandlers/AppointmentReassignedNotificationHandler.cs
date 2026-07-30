using MediatR;
using Microsoft.Extensions.Logging;
using Utano.Module.Core.Domain.Events.Appointments;
using Utano.Module.Core.Services;
using Utano.Module.Notifications.Domain.Entities;
using Utano.Module.Notifications.Domain.Enums;
using Utano.Module.Notifications.Domain.Interfaces;

namespace Utano.Module.Notifications.Features.AppointmentEventHandlers;

public class AppointmentReassignedNotificationHandler(
    INotificationRepository repository,
    ICurrentUserService currentUserService,
    ILogger<AppointmentReassignedNotificationHandler> logger)
    : INotificationHandler<AppointmentReassignedEvent>
{
    public async Task Handle(AppointmentReassignedEvent domainEvent, CancellationToken cancellationToken)
    {
        try
        {
            if (domainEvent.PreviousDoctorId != currentUserService.UserId)
            {
                var previousDoctorNotice = Notification.Create(
                    domainEvent.PracticeId,
                    domainEvent.PreviousDoctorId,
                    currentUserService.UserId,
                    currentUserService.FullName,
                    "Appointment reassigned",
                    $"{domainEvent.PatientName}'s appointment on {domainEvent.AppointmentDate:d MMM yyyy} was reassigned to {domainEvent.NewDoctorName}.",
                    NotificationType.AppointmentReassigned,
                    domainEvent.AppointmentId);
                await repository.AddAsync(previousDoctorNotice, cancellationToken);
            }

            if (domainEvent.NewDoctorId != currentUserService.UserId)
            {
                var newDoctorNotice = Notification.Create(
                    domainEvent.PracticeId,
                    domainEvent.NewDoctorId,
                    currentUserService.UserId,
                    currentUserService.FullName,
                    "New appointment assigned to you",
                    $"{domainEvent.PatientName}'s appointment on {domainEvent.AppointmentDate:d MMM yyyy} at {domainEvent.StartTime:h:mm tt} was reassigned to you.",
                    NotificationType.AppointmentReassigned,
                    domainEvent.AppointmentId);
                await repository.AddAsync(newDoctorNotice, cancellationToken);
            }

            await repository.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to create reassigned-appointment notification for appointment {AppointmentId}",
                domainEvent.AppointmentId);
        }
    }
}
