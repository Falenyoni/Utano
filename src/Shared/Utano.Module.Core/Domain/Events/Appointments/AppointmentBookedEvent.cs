namespace Utano.Module.Core.Domain.Events.Appointments;

public record AppointmentBookedEvent(
    Guid PracticeId,
    Guid AppointmentId,
    Guid DoctorId,
    string DoctorName,
    string PatientName,
    DateOnly AppointmentDate,
    TimeOnly StartTime,
    TimeOnly EndTime) : IDomainEvent;
