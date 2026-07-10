using MediatR;

namespace Utano.Module.Appointments.Features.Appointments.CancelAppointment;

public record CancelAppointmentCommand(Guid Id, string Reason) : IRequest;
