using MediatR;
using Utano.Module.Appointments.Domain.Enums;
using Utano.Module.Core.Models;

namespace Utano.Module.Appointments.Features.Appointments.GetAppointments;

public record GetAppointmentsQuery(
    DateOnly? Date,
    Guid? PatientId,
    Guid? DoctorId,
    AppointmentStatus? Status,
    int Page = 1,
    int PageSize = 20
) : IRequest<PagedResult<AppointmentSummaryResponse>>;
