using MediatR;
using Utano.Module.Appointments.Domain.Interfaces;
using Utano.Module.Core.Exceptions;

namespace Utano.Module.Appointments.Features.Appointments.CancelAppointment;

public class CancelAppointmentHandler(
    IAppointmentReadRepository readRepository,
    IAppointmentWriteRepository writeRepository)
    : IRequestHandler<CancelAppointmentCommand>
{
    public async Task Handle(CancelAppointmentCommand command, CancellationToken cancellationToken)
    {
        var appointment = await readRepository.GetByIdAsync(command.Id, cancellationToken);
        if (appointment is null)
            throw new UtanoDomainException("Appointment not found.");

        appointment.Cancel(command.Reason);
        await writeRepository.UpdateAsync(appointment, cancellationToken);
    }
}
