using MediatR;
using Utano.Module.Appointments.Domain.Interfaces;
using Utano.Module.Core.Exceptions;

namespace Utano.Module.Appointments.Features.Appointments.CheckInAppointment;

public class CheckInAppointmentHandler(
    IAppointmentReadRepository readRepository,
    IAppointmentWriteRepository writeRepository)
    : IRequestHandler<CheckInAppointmentCommand>
{
    public async Task Handle(CheckInAppointmentCommand command, CancellationToken cancellationToken)
    {
        var appointment = await readRepository.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new UtanoDomainException("Appointment not found.");

        appointment.CheckIn();
        await writeRepository.UpdateAsync(appointment, cancellationToken);
    }
}
