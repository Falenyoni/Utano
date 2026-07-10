using MediatR;

namespace Utano.Module.Appointments.Features.Appointments.GetAppointmentById;

public record GetAppointmentByIdQuery(Guid Id) : IRequest<GetAppointmentByIdResponse?>;
