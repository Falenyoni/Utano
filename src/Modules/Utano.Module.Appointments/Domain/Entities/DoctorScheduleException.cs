using Utano.Module.Appointments.Domain.Enums;
using Utano.Module.Core.Domain.Aggregate;

namespace Utano.Module.Appointments.Domain.Entities;

public class DoctorScheduleException : AggregateRoot
{
    private DoctorScheduleException() { }

    public Guid DoctorId { get; private set; }
    public DateOnly Date { get; private set; }
    public ScheduleExceptionType Type { get; private set; }
    public TimeOnly? StartTime { get; private set; }
    public TimeOnly? EndTime { get; private set; }
    public string? Reason { get; private set; }

    public static DoctorScheduleException Create(
        Guid practiceId,
        Guid doctorId,
        DateOnly date,
        ScheduleExceptionType type,
        TimeOnly? startTime = null,
        TimeOnly? endTime = null,
        string? reason = null)
    {
        return new DoctorScheduleException
        {
            Id = Guid.NewGuid(),
            PracticeId = practiceId,
            DoctorId = doctorId,
            Date = date,
            Type = type,
            StartTime = startTime,
            EndTime = endTime,
            Reason = reason,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
    }
}
