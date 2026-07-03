using Utano.Module.Patients.Domain.Entities;
using Utano.Module.Patients.Domain.Enums;

namespace Utano.Module.Patients.Domain.Interfaces;

public interface IPatientReadRepository
{
    Task<Patient?> GetByIdAsync(Guid id, Guid practiceId, CancellationToken cancellationToken = default);

    Task<IEnumerable<Patient>> GetAllAsync(Guid practiceId, PatientStatus? status, CancellationToken cancellationToken = default);

    Task<Patient?> GetByNationalIdAsync(string nationalId, Guid practiceId, CancellationToken cancellationToken = default);

    Task<IEnumerable<Patient>> SearchAsync(string searchTerm, Guid practiceId, CancellationToken cancellationToken = default);
}