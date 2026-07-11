using Utano.Module.Appointments.Domain.Enums;
using Utano.Module.Core.Domain.Aggregate;
using Utano.Module.Core.Exceptions;

namespace Utano.Module.Appointments.Domain.Entities;

public class Appointment : AggregateRoot
{
    private Appointment() { }

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
        string? notes = null)
    {
        if (practiceId == Guid.Empty) throw new UtanoDomainException("Practice is required.");
        if (patientId == Guid.Empty) throw new UtanoDomainException("Patient is required.");
        if (doctorId == Guid.Empty) throw new UtanoDomainException("Doctor is required.");
        if (endTime <= startTime) throw new UtanoDomainException("End time must be after start time.");

        return new Appointment
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
    }

    public void Confirm()
    {
        if (Status != AppointmentStatus.Scheduled)
            throw new UtanoDomainException("Only scheduled appointments can be confirmed.");
        Status = AppointmentStatus.Confirmed;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Cancel(string reason)
    {
        if (Status is AppointmentStatus.Completed or AppointmentStatus.Cancelled)
            throw new UtanoDomainException("Cannot cancel a completed or already cancelled appointment.");
        Status = AppointmentStatus.Cancelled;
        CancellationReason = reason;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkNoShow()
    {
        if (Status is not (AppointmentStatus.Scheduled or AppointmentStatus.Confirmed))
            throw new UtanoDomainException("Only scheduled or confirmed appointments can be marked as no-show.");
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
        if (Status is not (AppointmentStatus.Scheduled or AppointmentStatus.Confirmed))
            throw new UtanoDomainException("Only scheduled or confirmed appointments can be started.");
        Status = AppointmentStatus.InProgress;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Reschedule(DateOnly newDate, TimeOnly newStartTime, TimeOnly newEndTime)
    {
        if (Status is AppointmentStatus.Completed or AppointmentStatus.Cancelled)
            throw new UtanoDomainException("Cannot reschedule a completed or cancelled appointment.");
        if (newEndTime <= newStartTime)
            throw new UtanoDomainException("End time must be after start time.");

        AppointmentDate = newDate;
        StartTime = newStartTime;
        EndTime = newEndTime;
        Status = AppointmentStatus.Scheduled;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
