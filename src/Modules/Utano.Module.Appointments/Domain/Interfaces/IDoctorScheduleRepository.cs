using Utano.Module.Appointments.Domain.Entities;

namespace Utano.Module.Appointments.Domain.Interfaces;

public interface IDoctorScheduleRepository
{
    Task<IReadOnlyList<DoctorSchedule>> GetScheduleAsync(Guid practiceId, Guid doctorId, CancellationToken ct = default);

    Task<DoctorSchedule?> GetScheduleForDayAsync(Guid practiceId, Guid doctorId, DayOfWeek dayOfWeek, CancellationToken ct = default);

    Task ReplaceScheduleAsync(Guid practiceId, Guid doctorId, IEnumerable<DoctorSchedule> schedules, CancellationToken ct = default);

    Task<IReadOnlyList<DoctorScheduleException>> GetExceptionsAsync(Guid practiceId, Guid doctorId, CancellationToken ct = default);

    Task<DoctorScheduleException?> GetExceptionForDateAsync(Guid practiceId, Guid doctorId, DateOnly date, CancellationToken ct = default);

    Task AddExceptionAsync(DoctorScheduleException exception, CancellationToken ct = default);

    Task RemoveExceptionAsync(Guid id, Guid practiceId, CancellationToken ct = default);
}
