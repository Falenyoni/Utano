namespace Utano.Module.Core.Domain.Events.ClinicalNotes;

public record VisitClinicalNotesUpdatedEvent(
    Guid PracticeId,
    Guid VisitId,
    string PatientName) : IDomainEvent;
