using Shouldly;
using Utano.Module.Appointments.Domain.Entities;
using Utano.Module.Appointments.Domain.Enums;
using Utano.Module.Core.Domain.Events.Appointments;

namespace Utano.Module.Appointments.Tests.Domain;

public class AppointmentDomainEventsTests
{
    private static readonly DateOnly FutureDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));

    private static Appointment BookAppointment() =>
        Appointment.Book(
            practiceId: Guid.NewGuid(),
            patientId: Guid.NewGuid(),
            patientName: "Jane Doe",
            doctorId: Guid.NewGuid(),
            doctorName: "Dr. Smith",
            date: FutureDate,
            startTime: new TimeOnly(9, 0),
            endTime: new TimeOnly(9, 30),
            type: AppointmentType.Consultation);

    [Fact]
    public void Book_RaisesAppointmentBookedEvent()
    {
        var appointment = BookAppointment();

        appointment.DomainEvents.ShouldHaveSingleItem();
        appointment.DomainEvents[0].ShouldBeOfType<AppointmentBookedEvent>();
    }

    [Fact]
    public void Cancel_RaisesAppointmentCancelledEvent()
    {
        var appointment = BookAppointment();
        appointment.ClearDomainEvents();

        appointment.Cancel("Patient requested");

        appointment.DomainEvents.ShouldHaveSingleItem();
        var raised = appointment.DomainEvents[0].ShouldBeOfType<AppointmentCancelledEvent>();
        raised.Reason.ShouldBe("Patient requested");
    }

    [Fact]
    public void Reschedule_RaisesAppointmentRescheduledEvent()
    {
        var appointment = BookAppointment();
        appointment.ClearDomainEvents();
        var newDate = FutureDate.AddDays(1);

        appointment.Reschedule(newDate, new TimeOnly(10, 0), new TimeOnly(10, 30));

        appointment.DomainEvents.ShouldHaveSingleItem();
        var raised = appointment.DomainEvents[0].ShouldBeOfType<AppointmentRescheduledEvent>();
        raised.NewDate.ShouldBe(newDate);
    }

    [Fact]
    public void Reassign_RaisesAppointmentReassignedEventWithPreviousAndNewDoctor()
    {
        var appointment = BookAppointment();
        var originalDoctorId = appointment.DoctorId;
        appointment.ClearDomainEvents();
        var newDoctorId = Guid.NewGuid();

        appointment.Reassign(newDoctorId, "Dr. Jones");

        appointment.DomainEvents.ShouldHaveSingleItem();
        var raised = appointment.DomainEvents[0].ShouldBeOfType<AppointmentReassignedEvent>();
        raised.PreviousDoctorId.ShouldBe(originalDoctorId);
        raised.NewDoctorId.ShouldBe(newDoctorId);
    }

    [Fact]
    public void ClearDomainEvents_RemovesQueuedEvents()
    {
        var appointment = BookAppointment();

        appointment.ClearDomainEvents();

        appointment.DomainEvents.ShouldBeEmpty();
    }
}
