using MediatR;
using Utano.Module.Appointments.Domain.Enums;

namespace Utano.Module.Appointments.Features.Appointments.BookAppointment;

public record BookAppointmentCommand(
    Guid PatientId,
    string PatientName,
    Guid DoctorId,
    string DoctorName,
    DateOnly AppointmentDate,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string Type,
    string? Notes
) : IRequest<BookAppointmentResponse>;
