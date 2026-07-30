namespace Utano.Module.Core.Domain.Events.Appointments;

public record AppointmentRescheduledEvent(
    Guid PracticeId,
    Guid AppointmentId,
    Guid DoctorId,
    string DoctorName,
    string PatientName,
    DateOnly NewDate,
    TimeOnly NewStartTime,
    TimeOnly NewEndTime) : IDomainEvent;
