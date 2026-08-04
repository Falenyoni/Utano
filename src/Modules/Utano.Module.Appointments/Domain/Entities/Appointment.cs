using Utano.Module.Appointments.Domain.Enums;
using Utano.Module.Core.Domain.Aggregate;
using Utano.Module.Core.Domain.Events;
using Utano.Module.Core.Domain.Events.Appointments;
using Utano.Module.Core.Exceptions;

namespace Utano.Module.Appointments.Domain.Entities;

public class Appointment : AggregateRoot, IHasDomainEvents
{
    private Appointment() { }

    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear();
    private void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public Guid PatientId { get; private set; }
    public string PatientName { get; private set; } = null!;
    public Guid DoctorId { get; private set; }
    public string DoctorName { get; private set; } = null!;
    public DateOnly AppointmentDate { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }
    public AppointmentType Type { get; private set; }
    public AppointmentStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public string? CancellationReason { get; private set; }
    public DateTimeOffset? RemindedAt { get; private set; }

    // Computed, not stored - covers both "will become NoShow soon" (Scheduled/Confirmed, past end
    // time - AppointmentNoShowScanJob will pick these up) and "stuck, needs staff attention"
    // (CheckedIn/InProgress past end time - deliberately never auto-transitioned, since the
    // patient did show up). Single source of truth so GetAppointments/GetAppointmentById don't
    // duplicate this rule.
    public bool IsOverdue =>
        Status is AppointmentStatus.Scheduled or AppointmentStatus.Confirmed
            or AppointmentStatus.CheckedIn or AppointmentStatus.InProgress
        && new DateTimeOffset(AppointmentDate.ToDateTime(EndTime), TimeSpan.Zero) < DateTimeOffset.UtcNow;

    public static Appointment Book(
        Guid practiceId,
        Guid patientId,
        string patientName,
        Guid doctorId,
        string doctorName,
        DateOnly date,
        TimeOnly startTime,
        TimeOnly endTime,
        AppointmentType type,
        string? notes = null,
        bool allowPastDate = false)
    {
        if (practiceId == Guid.Empty) throw new UtanoDomainException("Practice is required.");
        if (patientId == Guid.Empty) throw new UtanoDomainException("Patient is required.");
        if (doctorId == Guid.Empty) throw new UtanoDomainException("Doctor is required.");
        if (!allowPastDate && date < DateOnly.FromDateTime(DateTime.UtcNow))
            throw new UtanoDomainException("Appointment date cannot be in the past.");
        if (endTime <= startTime) throw new UtanoDomainException("End time must be after start time.");

        var appointment = new Appointment
        {
            Id = Guid.NewGuid(),
            PracticeId = practiceId,
            PatientId = patientId,
            PatientName = patientName,
            DoctorId = doctorId,
            DoctorName = doctorName,
            AppointmentDate = date,
            StartTime = startTime,
            EndTime = endTime,
            Type = type,
            Status = AppointmentStatus.Scheduled,
            Notes = notes,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        appointment.AddDomainEvent(new AppointmentBookedEvent(
            practiceId, appointment.Id, doctorId, doctorName, patientName, date, startTime, endTime));

        return appointment;
    }

    public void Confirm()
    {
        if (Status != AppointmentStatus.Scheduled)
            throw new UtanoDomainException("Only scheduled appointments can be confirmed.");
        Status = AppointmentStatus.Confirmed;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void CheckIn()
    {
        if (Status is not (AppointmentStatus.Scheduled or AppointmentStatus.Confirmed))
            throw new UtanoDomainException("Only scheduled or confirmed appointments can be checked in.");
        Status = AppointmentStatus.CheckedIn;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Cancel(string reason)
    {
        if (Status is AppointmentStatus.Completed or AppointmentStatus.Cancelled)
            throw new UtanoDomainException("Cannot cancel a completed or already cancelled appointment.");
        Status = AppointmentStatus.Cancelled;
        CancellationReason = reason;
        UpdatedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new AppointmentCancelledEvent(
            PracticeId, Id, DoctorId, DoctorName, PatientName, AppointmentDate, StartTime, reason));
    }

    public void MarkNoShow()
    {
        if (Status is not (AppointmentStatus.Scheduled or AppointmentStatus.Confirmed or AppointmentStatus.CheckedIn))
            throw new UtanoDomainException("Only scheduled, confirmed, or checked-in appointments can be marked as no-show.");
        Status = AppointmentStatus.NoShow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Complete()
    {
        if (Status is not (AppointmentStatus.Scheduled or AppointmentStatus.Confirmed or AppointmentStatus.InProgress))
            throw new UtanoDomainException("Appointment cannot be completed from its current status.");
        Status = AppointmentStatus.Completed;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void StartVisit()
    {
        if (Status is not (AppointmentStatus.Scheduled or AppointmentStatus.Confirmed or AppointmentStatus.CheckedIn))
            throw new UtanoDomainException("Only scheduled, confirmed, or checked-in appointments can be started.");
        Status = AppointmentStatus.InProgress;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Reassign(Guid newDoctorId, string newDoctorName)
    {
        if (newDoctorId == Guid.Empty) throw new UtanoDomainException("Doctor is required.");
        if (Status is AppointmentStatus.Completed or AppointmentStatus.Cancelled)
            throw new UtanoDomainException("Cannot reassign a completed or cancelled appointment.");

        var previousDoctorId = DoctorId;
        var previousDoctorName = DoctorName;

        DoctorId = newDoctorId;
        DoctorName = newDoctorName;
        UpdatedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new AppointmentReassignedEvent(
            PracticeId, Id, previousDoctorId, previousDoctorName, newDoctorId, newDoctorName,
            PatientName, AppointmentDate, StartTime, EndTime));
    }

    public void Reschedule(DateOnly newDate, TimeOnly newStartTime, TimeOnly newEndTime)
    {
        if (Status is AppointmentStatus.Completed or AppointmentStatus.Cancelled)
            throw new UtanoDomainException("Cannot reschedule a completed or cancelled appointment.");
        if (newDate < DateOnly.FromDateTime(DateTime.UtcNow))
            throw new UtanoDomainException("Appointment date cannot be in the past.");
        if (newEndTime <= newStartTime)
            throw new UtanoDomainException("End time must be after start time.");

        AppointmentDate = newDate;
        StartTime = newStartTime;
        EndTime = newEndTime;
        Status = AppointmentStatus.Scheduled;
        RemindedAt = null; // moved to a new time - due for its own reminder again
        UpdatedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new AppointmentRescheduledEvent(
            PracticeId, Id, DoctorId, DoctorName, PatientName, newDate, newStartTime, newEndTime));
    }

    public void MarkReminded()
    {
        if (RemindedAt.HasValue) return;
        RemindedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new AppointmentReminderDueEvent(
            PracticeId, Id, DoctorId, DoctorName, PatientName, AppointmentDate, StartTime));
    }
}
