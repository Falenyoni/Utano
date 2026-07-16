using MediatR;

namespace Utano.Module.Appointments.Features.Appointments.CheckInAppointment;

public record CheckInAppointmentCommand(Guid Id) : IRequest;
