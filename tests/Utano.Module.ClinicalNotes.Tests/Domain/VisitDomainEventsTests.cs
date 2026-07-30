using Shouldly;
using Utano.Module.ClinicalNotes.Domain.Entities;
using Utano.Module.Core.Domain.Events.ClinicalNotes;

namespace Utano.Module.ClinicalNotes.Tests.Domain;

public class VisitDomainEventsTests
{
    private static Visit OpenVisit() =>
        Visit.Open(
            practiceId: Guid.NewGuid(),
            patientId: Guid.NewGuid(),
            patientName: "Jane Doe",
            doctorId: Guid.NewGuid(),
            doctorName: "Dr. Smith",
            visitDate: DateOnly.FromDateTime(DateTime.UtcNow));

    [Fact]
    public void Triage_RaisesVisitTriagedEvent()
    {
        var visit = OpenVisit();

        visit.Triage(120, 80, 70, 170, 36.6m, 72, 98, "Headache");

        visit.DomainEvents.ShouldHaveSingleItem();
        visit.DomainEvents[0].ShouldBeOfType<VisitTriagedEvent>();
    }

    [Fact]
    public void Complete_RaisesVisitCompletedEvent()
    {
        var visit = OpenVisit();
        visit.ClearDomainEvents();

        visit.Complete();

        visit.DomainEvents.ShouldHaveSingleItem();
        var raised = visit.DomainEvents[0].ShouldBeOfType<VisitCompletedEvent>();
        raised.PatientName.ShouldBe("Jane Doe");
        raised.DoctorName.ShouldBe("Dr. Smith");
    }

    [Fact]
    public void UpdateClinicalNotes_RaisesVisitClinicalNotesUpdatedEvent()
    {
        var visit = OpenVisit();
        visit.ClearDomainEvents();

        visit.UpdateClinicalNotes(
            chiefComplaint: "Fever", symptoms: "Chills",
            diagnosis: "Flu", treatment: "Rest and fluids",
            prescription: "Paracetamol", notes: null);

        visit.DomainEvents.ShouldHaveSingleItem();
        visit.DomainEvents[0].ShouldBeOfType<VisitClinicalNotesUpdatedEvent>();
    }
}
