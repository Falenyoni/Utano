using Microsoft.EntityFrameworkCore;
using Utano.Module.Appointments.DatabaseMappings;
using Utano.Module.Appointments.Domain.Entities;
using Utano.Module.Appointments.Domain.Interfaces;

namespace Utano.Module.Appointments.Infrastructure.Repositories;

public class DoctorScheduleRepository(AppointmentsDbContext context) : IDoctorScheduleRepository
{
    public async Task<IReadOnlyList<DoctorSchedule>> GetScheduleAsync(Guid practiceId, Guid doctorId, CancellationToken ct = default)
        => await context.DoctorSchedules
            .AsNoTracking()
            .Where(s => s.DoctorId == doctorId)
            .OrderBy(s => s.DayOfWeek)
            .ToListAsync(ct);

    public async Task<DoctorSchedule?> GetScheduleForDayAsync(Guid practiceId, Guid doctorId, DayOfWeek dayOfWeek, CancellationToken ct = default)
        => await context.DoctorSchedules
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.DoctorId == doctorId && s.DayOfWeek == dayOfWeek, ct);

    public async Task ReplaceScheduleAsync(Guid practiceId, Guid doctorId, IEnumerable<DoctorSchedule> schedules, CancellationToken ct = default)
    {
        var existing = await context.DoctorSchedules
            .Where(s => s.DoctorId == doctorId)
            .ToListAsync(ct);
        context.DoctorSchedules.RemoveRange(existing);
        await context.DoctorSchedules.AddRangeAsync(schedules, ct);
        await context.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<DoctorScheduleException>> GetExceptionsAsync(Guid practiceId, Guid doctorId, CancellationToken ct = default)
        => await context.DoctorScheduleExceptions
            .AsNoTracking()
            .Where(e => e.DoctorId == doctorId)
            .OrderBy(e => e.Date)
            .ToListAsync(ct);

    public async Task<DoctorScheduleException?> GetExceptionForDateAsync(Guid practiceId, Guid doctorId, DateOnly date, CancellationToken ct = default)
        => await context.DoctorScheduleExceptions
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.DoctorId == doctorId && e.Date == date, ct);

    public async Task AddExceptionAsync(DoctorScheduleException exception, CancellationToken ct = default)
    {
        await context.DoctorScheduleExceptions.AddAsync(exception, ct);
        await context.SaveChangesAsync(ct);
    }

    public async Task RemoveExceptionAsync(Guid id, Guid practiceId, CancellationToken ct = default)
    {
        var entity = await context.DoctorScheduleExceptions
            .FirstOrDefaultAsync(e => e.Id == id, ct);
        if (entity is not null)
        {
            context.DoctorScheduleExceptions.Remove(entity);
            await context.SaveChangesAsync(ct);
        }
    }
}
