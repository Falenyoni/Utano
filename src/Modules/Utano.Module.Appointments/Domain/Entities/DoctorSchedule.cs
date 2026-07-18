using Utano.Module.Core.Domain.Aggregate;

namespace Utano.Module.Appointments.Domain.Entities;

public class DoctorSchedule : AggregateRoot
{
    private DoctorSchedule() { }

    public Guid DoctorId { get; private set; }
    public DayOfWeek DayOfWeek { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }
    public int SlotDurationMinutes { get; private set; }
    public bool IsActive { get; private set; }

    public static DoctorSchedule Create(
        Guid practiceId,
        Guid doctorId,
        DayOfWeek dayOfWeek,
        TimeOnly startTime,
        TimeOnly endTime,
        int slotDurationMinutes = 30,
        bool isActive = true)
    {
        return new DoctorSchedule
        {
            Id = Guid.NewGuid(),
            PracticeId = practiceId,
            DoctorId = doctorId,
            DayOfWeek = dayOfWeek,
            StartTime = startTime,
            EndTime = endTime,
            SlotDurationMinutes = slotDurationMinutes,
            IsActive = isActive,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
    }

    public void Update(TimeOnly startTime, TimeOnly endTime, int slotDurationMinutes, bool isActive)
    {
        StartTime = startTime;
        EndTime = endTime;
        SlotDurationMinutes = slotDurationMinutes;
        IsActive = isActive;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
