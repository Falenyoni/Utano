using Microsoft.EntityFrameworkCore;
using Utano.Module.Patients.DatabaseMappings;
using Utano.Module.Patients.Domain.Entities;
using Utano.Module.Patients.Domain.Interfaces;

namespace Utano.Module.Patients.Infrastructure.Repositories;

public class MedicalAidRepository(PatientsDbContext context) : IMedicalAidRepository
{
    public async Task<IReadOnlyList<MedicalAid>> GetAllAsync(CancellationToken cancellationToken = default)
        => await context.MedicalAids
            .AsNoTracking()
            .OrderBy(m => m.Name)
            .ToListAsync(cancellationToken);

    public async Task<MedicalAid?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await context.MedicalAids
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    public async Task AddAsync(MedicalAid medicalAid, CancellationToken cancellationToken = default)
    {
        await context.MedicalAids.AddAsync(medicalAid, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
        => await context.SaveChangesAsync(cancellationToken);
}
