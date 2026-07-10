using MediatR;
using Utano.Module.Appointments.Domain.Interfaces;
using Utano.Module.Core.Exceptions;

namespace Utano.Module.Appointments.Features.Appointments.GetAppointmentById;

public class GetAppointmentByIdHandler(IAppointmentReadRepository readRepository)
    : IRequestHandler<GetAppointmentByIdQuery, GetAppointmentByIdResponse?>
{
    public async Task<GetAppointmentByIdResponse?> Handle(
        GetAppointmentByIdQuery query, CancellationToken cancellationToken)
    {
        var appointment = await readRepository.GetByIdAsync(query.Id, cancellationToken);
        if (appointment is null) return null;

        return new GetAppointmentByIdResponse(
            appointment.Id,
            appointment.PatientId,
            appointment.PatientName,
            appointment.DoctorId,
            appointment.DoctorName,
            appointment.AppointmentDate,
            appointment.StartTime,
            appointment.EndTime,
            appointment.Type.ToString(),
            appointment.Status.ToString(),
            appointment.Notes,
            appointment.CancellationReason,
            appointment.CreatedAt,
            appointment.UpdatedAt);
    }
}
