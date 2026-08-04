using Microsoft.EntityFrameworkCore;
using Utano.Module.Appointments.DatabaseMappings;
using Utano.Module.Appointments.Domain.Entities;
using Utano.Module.Appointments.Domain.Enums;
using Utano.Module.Appointments.Domain.Interfaces;
using Utano.Module.Core.Models;

namespace Utano.Module.Appointments.Infrastructure.Repositories;

public class AppointmentReadRepository(AppointmentsDbContext context) : IAppointmentReadRepository
{
    public async Task<Appointment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await context.Appointments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<PagedResult<Appointment>> GetPagedAsync(
        DateOnly? date,
        Guid? patientId,
        Guid? doctorId,
        AppointmentStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = context.Appointments.AsNoTracking();

        if (date.HasValue)
            query = query.Where(a => a.AppointmentDate == date.Value);

        if (patientId.HasValue)
            query = query.Where(a => a.PatientId == patientId.Value);

        if (doctorId.HasValue)
            query = query.Where(a => a.DoctorId == doctorId.Value);

        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(a => a.AppointmentDate)
            .ThenBy(a => a.StartTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Appointment>
        {
            Data = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<IReadOnlyList<Appointment>> GetByDoctorDateAsync(
        Guid practiceId,
        Guid doctorId,
        DateOnly date,
        CancellationToken cancellationToken = default)
        => await context.Appointments
            .AsNoTracking()
            .Where(a =>
                a.DoctorId == doctorId &&
                a.AppointmentDate == date &&
                a.Status != AppointmentStatus.Cancelled &&
                a.Status != AppointmentStatus.Completed)
            .OrderBy(a => a.StartTime)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Appointment>> GetAppointmentsNeedingReminderAsync(
        DateTimeOffset remindByCutoff, CancellationToken cancellationToken = default)
    {
        var nowUtc = DateTimeOffset.UtcNow;

        // Coarse filter in SQL (date range only - DateOnly/TimeOnly combination isn't reliably
        // translatable), then combine date+time precisely in memory. IgnoreQueryFilters is
        // deliberate: this runs from a background job with no per-request practice context, and
        // needs to see every practice's appointments, not just one.
        var candidates = await context.Appointments
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(a =>
                (a.Status == AppointmentStatus.Scheduled || a.Status == AppointmentStatus.Confirmed) &&
                a.RemindedAt == null &&
                a.AppointmentDate >= DateOnly.FromDateTime(nowUtc.UtcDateTime) &&
                a.AppointmentDate <= DateOnly.FromDateTime(remindByCutoff.UtcDateTime))
            .ToListAsync(cancellationToken);

        return candidates
            .Where(a =>
            {
                var startsAt = new DateTimeOffset(a.AppointmentDate.ToDateTime(a.StartTime), TimeSpan.Zero);
                return startsAt > nowUtc && startsAt <= remindByCutoff;
            })
            .ToList();
    }

    public async Task<IReadOnlyList<Appointment>> GetAppointmentsPastNoShowGraceAsync(
        TimeSpan gracePeriod, CancellationToken cancellationToken = default)
    {
        var nowUtc = DateTimeOffset.UtcNow;

        // Same coarse-SQL-filter-then-precise-in-memory pattern as the reminder scan - DateOnly/
        // TimeOnly combination isn't reliably translatable to SQL. IgnoreQueryFilters deliberate:
        // background job, no per-request practice context.
        var candidates = await context.Appointments
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(a =>
                (a.Status == AppointmentStatus.Scheduled || a.Status == AppointmentStatus.Confirmed) &&
                a.AppointmentDate <= DateOnly.FromDateTime(nowUtc.UtcDateTime))
            .ToListAsync(cancellationToken);

        return candidates
            .Where(a =>
            {
                var endsAt = new DateTimeOffset(a.AppointmentDate.ToDateTime(a.EndTime), TimeSpan.Zero);
                return endsAt.Add(gracePeriod) <= nowUtc;
            })
            .ToList();
    }

    public async Task<bool> HasConflictAsync(
        Guid practiceId,
        Guid doctorId,
        DateOnly date,
        TimeOnly startTime,
        TimeOnly endTime,
        Guid? excludeAppointmentId = null,
        CancellationToken cancellationToken = default)
    {
        return await context.Appointments
            .AsNoTracking()
            .Where(a =>
                a.PracticeId == practiceId &&
                a.DoctorId == doctorId &&
                a.AppointmentDate == date &&
                a.Status != AppointmentStatus.Cancelled &&
                a.Status != AppointmentStatus.Completed &&
                (excludeAppointmentId == null || a.Id != excludeAppointmentId) &&
                a.StartTime < endTime &&
                a.EndTime > startTime)
            .AnyAsync(cancellationToken);
    }
}
