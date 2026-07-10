namespace Utano.Module.Appointments.Features.Appointments.BookAppointment;

public record BookAppointmentResponse(
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
    DateTimeOffset CreatedAt
);
