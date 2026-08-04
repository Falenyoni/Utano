using Utano.Module.Appointments.Domain.Entities;
using Utano.Module.Appointments.Domain.Enums;
using Utano.Module.Core.Models;

namespace Utano.Module.Appointments.Domain.Interfaces;

public interface IAppointmentReadRepository
{
    Task<Appointment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// When onlyOverdue is true, date is ignored (overdue is inherently cross-date) and the
    /// result is IsOverdue-filtered practice-wide instead of date-scoped.
    /// </summary>
    Task<PagedResult<Appointment>> GetPagedAsync(
        DateOnly? date,
        Guid? patientId,
        Guid? doctorId,
        AppointmentStatus? status,
        bool onlyOverdue,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<bool> HasConflictAsync(
        Guid practiceId,
        Guid doctorId,
        DateOnly date,
        TimeOnly startTime,
        TimeOnly endTime,
        Guid? excludeAppointmentId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Appointment>> GetByDoctorDateAsync(
        Guid practiceId,
        Guid doctorId,
        DateOnly date,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Across ALL practices - for the reminder scan background job, which has no
    /// per-request practice context to scope to. Bypasses the tenant query filter deliberately.
    /// </summary>
    Task<IReadOnlyList<Appointment>> GetAppointmentsNeedingReminderAsync(
        DateTimeOffset remindByCutoff,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Across ALL practices - for the no-show scan background job. Only Scheduled/Confirmed
    /// (never checked in) appointments whose end time is more than gracePeriod in the past -
    /// CheckedIn/InProgress are deliberately excluded here since the patient did show up; those
    /// are surfaced as "overdue" for staff attention instead, never auto-transitioned.
    /// </summary>
    Task<IReadOnlyList<Appointment>> GetAppointmentsPastNoShowGraceAsync(
        TimeSpan gracePeriod,
        CancellationToken cancellationToken = default);
}
