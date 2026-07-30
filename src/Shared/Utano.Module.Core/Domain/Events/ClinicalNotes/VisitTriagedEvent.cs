namespace Utano.Module.Core.Domain.Events.ClinicalNotes;

public record VisitTriagedEvent(
    Guid PracticeId,
    Guid VisitId,
    string PatientName) : IDomainEvent;
