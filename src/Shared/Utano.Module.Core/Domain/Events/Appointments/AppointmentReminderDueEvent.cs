namespace Utano.Module.Core.Domain.Events.Appointments;

public record AppointmentReminderDueEvent(
    Guid PracticeId,
    Guid AppointmentId,
    Guid DoctorId,
    string DoctorName,
    Guid PatientId,
    string PatientName,
    DateOnly AppointmentDate,
    TimeOnly StartTime) : IDomainEvent;
