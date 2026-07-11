using Utano.Module.Appointments.Domain.Interfaces;
using Utano.Module.Core.Services;

namespace Utano.Module.Appointments.Infrastructure.Services;

public class AppointmentLinker(
    IAppointmentReadRepository readRepository,
    IAppointmentWriteRepository writeRepository) : IAppointmentLinker
{
    public async Task MarkInProgressAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
        var appointment = await readRepository.GetByIdAsync(appointmentId, cancellationToken);
        if (appointment is null) return;
        appointment.StartVisit();
        await writeRepository.UpdateAsync(appointment, cancellationToken);
    }

    public async Task MarkCompletedAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
        var appointment = await readRepository.GetByIdAsync(appointmentId, cancellationToken);
        if (appointment is null) return;
        appointment.Complete();
        await writeRepository.UpdateAsync(appointment, cancellationToken);
    }
}
