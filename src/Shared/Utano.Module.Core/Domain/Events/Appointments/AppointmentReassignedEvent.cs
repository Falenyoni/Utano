namespace Utano.Module.Core.Domain.Events.Appointments;

public record AppointmentReassignedEvent(
    Guid PracticeId,
    Guid AppointmentId,
    Guid PreviousDoctorId,
    string PreviousDoctorName,
    Guid NewDoctorId,
    string NewDoctorName,
    string PatientName,
    DateOnly AppointmentDate,
    TimeOnly StartTime,
    TimeOnly EndTime) : IDomainEvent;
