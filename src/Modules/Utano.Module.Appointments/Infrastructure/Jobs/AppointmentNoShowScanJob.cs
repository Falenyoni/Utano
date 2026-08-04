using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Utano.Module.Appointments.Configuration;
using Utano.Module.Appointments.Domain.Interfaces;

namespace Utano.Module.Appointments.Infrastructure.Jobs;

// Runs on a Hangfire recurring schedule (see ConfigureAppointmentsModule). Auto-marks Scheduled/
// Confirmed appointments as NoShow once GraceMinutes have passed since their scheduled end time
// with no check-in. Deliberately does NOT touch CheckedIn/InProgress appointments - the patient
// did show up in those cases, so "no-show" would be factually wrong; those are surfaced as
// "overdue" for staff attention instead (see AppointmentSummaryResponse.IsOverdue), never
// auto-transitioned.
public class AppointmentNoShowScanJob(
    IAppointmentReadRepository readRepository,
    IAppointmentWriteRepository writeRepository,
    IOptions<AppointmentNoShowSettings> settings,
    ILogger<AppointmentNoShowScanJob> logger)
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var gracePeriod = TimeSpan.FromMinutes(settings.Value.GraceMinutes);
        var overdueAppointments = await readRepository.GetAppointmentsPastNoShowGraceAsync(gracePeriod, cancellationToken);

        foreach (var appointment in overdueAppointments)
        {
            try
            {
                appointment.MarkNoShow();
                await writeRepository.UpdateAsync(appointment, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to mark no-show for appointment {AppointmentId}", appointment.Id);
            }
        }
    }
}
