namespace Utano.Module.Appointments.Features.Appointments.GetAppointmentById;

public record GetAppointmentByIdResponse(
    Guid Id,
    Guid PatientId,
    string PatientName,
    Guid DoctorId,
    string DoctorName,
    DateOnly AppointmentDate,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string Type,
    string Status,
    string? Notes,
    string? CancellationReason,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);
