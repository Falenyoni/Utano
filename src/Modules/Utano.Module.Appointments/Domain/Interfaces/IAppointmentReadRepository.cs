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
}
