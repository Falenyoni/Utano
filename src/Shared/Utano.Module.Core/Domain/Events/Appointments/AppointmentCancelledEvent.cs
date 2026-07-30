namespace Utano.Module.Core.Domain.Events.Appointments;

public record AppointmentCancelledEvent(
    Guid PracticeId,
    Guid AppointmentId,
    Guid DoctorId,
    string DoctorName,
    string PatientName,
    DateOnly AppointmentDate,
    TimeOnly StartTime,
    string Reason) : IDomainEvent;
