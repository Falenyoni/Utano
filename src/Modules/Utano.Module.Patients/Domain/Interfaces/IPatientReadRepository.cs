using Utano.Module.Core.Models;
using Utano.Module.Patients.Domain.Entities;
using Utano.Module.Patients.Domain.Enums;

namespace Utano.Module.Patients.Domain.Interfaces;

public interface IPatientReadRepository
{
    Task<Patient?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Patient?> GetByNationalIdAsync(string nationalId, CancellationToken cancellationToken = default);

    Task<PagedResult<Patient>> GetPagedAsync(string? searchTerm, PatientStatus? status,
        int page, int pageSize, CancellationToken cancellationToken = default);
}
