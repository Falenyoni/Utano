using Utano.Module.Appointments.Domain.Entities;
using Utano.Module.Appointments.Domain.Enums;
using Utano.Module.Core.Models;

namespace Utano.Module.Appointments.Domain.Interfaces;

public interface IAppointmentReadRepository
{
    Task<Appointment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<Appointment>> GetPagedAsync(
        DateOnly? date,
        Guid? patientId,
        Guid? doctorId,
        AppointmentStatus? status,
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
}
