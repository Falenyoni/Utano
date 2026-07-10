using Utano.Module.Appointments.Domain.Entities;

namespace Utano.Module.Appointments.Domain.Interfaces;

public interface IAppointmentWriteRepository
{
    Task AddAsync(Appointment appointment, CancellationToken cancellationToken = default);
    Task UpdateAsync(Appointment appointment, CancellationToken cancellationToken = default);
}
