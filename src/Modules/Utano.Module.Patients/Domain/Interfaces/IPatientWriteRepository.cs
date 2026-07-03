using Utano.Module.Patients.Domain.Entities;

namespace Utano.Module.Patients.Domain.Interfaces;

public interface IPatientWriteRepository
{
    Task AddAsync(Patient patient, CancellationToken cancellationToken = default);

    Task UpdateAsync(Patient patient, CancellationToken cancellationToken = default);
}