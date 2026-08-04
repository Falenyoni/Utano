using MediatR;
using Utano.Module.Appointments.Domain.Interfaces;
using Utano.Module.Core.Exceptions;
using Utano.Module.Core.Services;

namespace Utano.Module.Appointments.Features.Appointments.GetAppointmentById;

public class GetAppointmentByIdHandler(
    IAppointmentReadRepository readRepository,
    IVisitLookup visitLookup,
    IPatientDemographicsLookup demographicsLookup)
    : IRequestHandler<GetAppointmentByIdQuery, GetAppointmentByIdResponse?>
{
    public async Task<GetAppointmentByIdResponse?> Handle(
        GetAppointmentByIdQuery query, CancellationToken cancellationToken)
    {
        var appointment = await readRepository.GetByIdAsync(query.Id, cancellationToken);
        if (appointment is null) return null;

        var visitIds = await visitLookup.GetVisitIdsForAppointmentsAsync([appointment.Id], cancellationToken);
        var demographics = await demographicsLookup.GetDemographicsAsync([appointment.PatientId], cancellationToken);
        demographics.TryGetValue(appointment.PatientId, out var d);

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
            appointment.UpdatedAt,
            visitIds.TryGetValue(appointment.Id, out var visitId) ? visitId : null,
            d?.Gender,
            d?.DateOfBirth,
            appointment.IsOverdue);
    }
}
