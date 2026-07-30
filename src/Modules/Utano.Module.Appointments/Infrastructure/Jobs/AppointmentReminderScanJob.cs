using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Utano.Module.Appointments.Configuration;
using Utano.Module.Appointments.Domain.Interfaces;

namespace Utano.Module.Appointments.Infrastructure.Jobs;

// Runs on a Hangfire recurring schedule (see ConfigureAppointmentsModule). Finds appointments
// entering the reminder window and marks them - Appointment.MarkReminded() queues an
// AppointmentReminderDueEvent, which the already-registered DomainEventDispatchInterceptor
// publishes as soon as writeRepository.UpdateAsync saves. No direct IPublisher call needed here.
public class AppointmentReminderScanJob(
    IAppointmentReadRepository readRepository,
    IAppointmentWriteRepository writeRepository,
    IOptions<AppointmentReminderSettings> settings,
    ILogger<AppointmentReminderScanJob> logger)
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var cutoff = DateTimeOffset.UtcNow.AddHours(settings.Value.HoursBefore);
        var dueAppointments = await readRepository.GetAppointmentsNeedingReminderAsync(cutoff, cancellationToken);

        foreach (var appointment in dueAppointments)
        {
            try
            {
                appointment.MarkReminded();
                await writeRepository.UpdateAsync(appointment, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to mark reminder for appointment {AppointmentId}", appointment.Id);
            }
        }
    }
}
