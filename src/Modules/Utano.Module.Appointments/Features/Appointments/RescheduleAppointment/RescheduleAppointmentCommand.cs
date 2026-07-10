using MediatR;

namespace Utano.Module.Appointments.Features.Appointments.RescheduleAppointment;

public record RescheduleAppointmentCommand(
    Guid Id,
    DateOnly NewDate,
    TimeOnly NewStartTime,
    TimeOnly NewEndTime
) : IRequest;
