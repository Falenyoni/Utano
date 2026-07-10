using MediatR;
using Utano.Module.Appointments.Domain.Interfaces;
using Utano.Module.Core.Exceptions;

namespace Utano.Module.Appointments.Features.Appointments.RescheduleAppointment;

public class RescheduleAppointmentHandler(
    IAppointmentReadRepository readRepository,
    IAppointmentWriteRepository writeRepository)
    : IRequestHandler<RescheduleAppointmentCommand>
{
    public async Task Handle(RescheduleAppointmentCommand command, CancellationToken cancellationToken)
    {
        var appointment = await readRepository.GetByIdAsync(command.Id, cancellationToken);
        if (appointment is null)
            throw new UtanoDomainException("Appointment not found.");

        appointment.Reschedule(command.NewDate, command.NewStartTime, command.NewEndTime);
        await writeRepository.UpdateAsync(appointment, cancellationToken);
    }
}
