using MediatR;
using Utano.Module.Appointments.Domain.Interfaces;
using Utano.Module.Core.Exceptions;

namespace Utano.Module.Appointments.Features.Appointments.ReassignAppointment;

public class ReassignAppointmentHandler(
    IAppointmentReadRepository readRepository,
    IAppointmentWriteRepository writeRepository)
    : IRequestHandler<ReassignAppointmentCommand>
{
    public async Task Handle(ReassignAppointmentCommand command, CancellationToken cancellationToken)
    {
        var appointment = await readRepository.GetByIdAsync(command.Id, cancellationToken);
        if (appointment is null)
            throw new UtanoDomainException("Appointment not found.");

        // Check the new doctor doesn't already have a conflicting slot on that date/time
        var hasConflict = await readRepository.HasConflictAsync(
            appointment.PracticeId,
            command.NewDoctorId,
            appointment.AppointmentDate,
            appointment.StartTime,
            appointment.EndTime,
            cancellationToken: cancellationToken);
        if (hasConflict)
            throw new UtanoDomainException("The selected doctor already has an appointment in that time slot.");

        appointment.Reassign(command.NewDoctorId, command.NewDoctorName);
        await writeRepository.UpdateAsync(appointment, cancellationToken);
    }
}
