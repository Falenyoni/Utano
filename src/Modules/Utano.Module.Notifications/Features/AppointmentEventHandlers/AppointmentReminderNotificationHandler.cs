using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Utano.Module.Core.Domain.Events.Appointments;
using Utano.Module.Core.Services;
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
    IUserContactLookup userContactLookup,
    IPatientContactLookup patientContactLookup,
    IEmailSender emailSender,
    ILogger<AppointmentReminderNotificationHandler> logger)
    : INotificationHandler<AppointmentReminderDueEvent>
{
    public async Task Handle(AppointmentReminderDueEvent domainEvent, CancellationToken cancellationToken)
    {
        var preference = await db.NotificationPreferences
            .IgnoreQueryFilters() // background job - no per-request practice context
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == domainEvent.DoctorId, cancellationToken);

        await CreateInAppNotificationAsync(domainEvent, preference?.InAppEnabled ?? true, cancellationToken);
        await SendDoctorEmailAsync(domainEvent, preference?.EmailEnabled ?? false, cancellationToken);
        await SendPatientEmailAsync(domainEvent, cancellationToken);
    }

    private async Task CreateInAppNotificationAsync(
        AppointmentReminderDueEvent domainEvent, bool inAppEnabled, CancellationToken ct)
    {
        if (!inAppEnabled) return;

        try
        {
            var entity = Notification.Create(
                domainEvent.PracticeId,
                domainEvent.DoctorId,
                Guid.Empty,
                "System",
                "Upcoming appointment reminder",
                $"{domainEvent.PatientName} on {domainEvent.AppointmentDate:d MMM yyyy} at {domainEvent.StartTime:h:mm tt}.",
                NotificationType.AppointmentReminder,
                domainEvent.AppointmentId);

            await repository.AddAsync(entity, ct);
            await repository.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to create reminder notification for appointment {AppointmentId}",
                domainEvent.AppointmentId);
        }
    }

    private async Task SendDoctorEmailAsync(
        AppointmentReminderDueEvent domainEvent, bool emailEnabled, CancellationToken ct)
    {
        if (!emailEnabled) return;

        try
        {
            var doctorEmail = await userContactLookup.GetEmailAsync(domainEvent.DoctorId, ct);
            if (doctorEmail is null) return;

            await emailSender.SendAsync(
                doctorEmail,
                "Upcoming appointment reminder",
                $"""
                <p>Hi {domainEvent.DoctorName},</p>
                <p>Reminder: you have an appointment with {domainEvent.PatientName} on
                {domainEvent.AppointmentDate:d MMM yyyy} at {domainEvent.StartTime:h:mm tt}.</p>
                """,
                ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to send doctor reminder email for appointment {AppointmentId}",
                domainEvent.AppointmentId);
        }
    }

    private async Task SendPatientEmailAsync(AppointmentReminderDueEvent domainEvent, CancellationToken ct)
    {
        try
        {
            var patientEmail = await patientContactLookup.GetEmailAsync(domainEvent.PatientId, ct);
            if (patientEmail is null) return; // no email on file - SMS/WhatsApp will cover this once built

            await emailSender.SendAsync(
                patientEmail,
                "Appointment reminder",
                $"""
                <p>Hi {domainEvent.PatientName},</p>
                <p>This is a reminder of your appointment with {domainEvent.DoctorName} on
                {domainEvent.AppointmentDate:d MMM yyyy} at {domainEvent.StartTime:h:mm tt}.</p>
                <p>If you need to reschedule, please contact the practice directly.</p>
                """,
                ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to send patient reminder email for appointment {AppointmentId}",
                domainEvent.AppointmentId);
        }
    }
}
