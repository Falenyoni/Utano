using MediatR;

namespace Utano.Module.Appointments.Features.Appointments.UndoNoShowAppointment;

public record UndoNoShowAppointmentCommand(Guid Id) : IRequest;
