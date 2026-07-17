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

        var hasConflict = await readRepository.HasConflictAsync(
            appointment.PracticeId,
            appointment.DoctorId,
            command.NewDate,
            command.NewStartTime,
            command.NewEndTime,
            excludeAppointmentId: command.Id,
            cancellationToken: cancellationToken);
        if (hasConflict)
            throw new UtanoDomainException("The doctor already has an appointment in that time slot.");

        appointment.Reschedule(command.NewDate, command.NewStartTime, command.NewEndTime);
        await writeRepository.UpdateAsync(appointment, cancellationToken);
    }
}
