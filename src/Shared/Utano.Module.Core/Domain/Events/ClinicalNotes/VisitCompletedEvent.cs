namespace Utano.Module.Core.Domain.Events.ClinicalNotes;

public record VisitCompletedEvent(
    Guid PracticeId,
    Guid VisitId,
    string PatientName,
    string DoctorName) : IDomainEvent;
