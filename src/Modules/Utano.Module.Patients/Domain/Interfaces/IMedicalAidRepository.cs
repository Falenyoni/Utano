using Utano.Module.Patients.Domain.Entities;

namespace Utano.Module.Patients.Domain.Interfaces;

public interface IMedicalAidRepository
{
    Task<IReadOnlyList<MedicalAid>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<MedicalAid?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(MedicalAid medicalAid, CancellationToken cancellationToken = default);
    Task SaveAsync(CancellationToken cancellationToken = default);
}
