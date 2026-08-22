using MediatR;
using Utano.Module.Appointments.Domain.Interfaces;
using Utano.Module.Core.Exceptions;

namespace Utano.Module.Appointments.Features.Appointments.UndoNoShowAppointment;

public class UndoNoShowAppointmentHandler(
    IAppointmentReadRepository readRepository,
    IAppointmentWriteRepository writeRepository)
    : IRequestHandler<UndoNoShowAppointmentCommand>
{
    public async Task Handle(UndoNoShowAppointmentCommand command, CancellationToken cancellationToken)
    {
        var appointment = await readRepository.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new UtanoDomainException("Appointment not found.");

        appointment.UndoNoShow();
        await writeRepository.UpdateAsync(appointment, cancellationToken);
    }
}
