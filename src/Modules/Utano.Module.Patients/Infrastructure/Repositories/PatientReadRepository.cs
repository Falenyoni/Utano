using Microsoft.EntityFrameworkCore;
using Utano.Module.Patients.DatabaseMappings;
using Utano.Module.Patients.Domain.Entities;
using Utano.Module.Patients.Domain.Enums;
using Utano.Module.Patients.Domain.Interfaces;

namespace Utano.Module.Patients.Infrastructure.Repositories;

public class PatientReadRepository(PatientsDbContext context) : IPatientReadRepository
{
    public async Task<Patient?> GetByIdAsync(Guid id, Guid practiceId, CancellationToken cancellationToken = default)
    {
        return await context.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Patient>> GetAllAsync(Guid practiceId, PatientStatus? status, CancellationToken cancellationToken = default)
    {
        var query = context.Patients.AsNoTracking();

        if (status.HasValue)
        {
            query = query.Where(e => e.Status == status.Value);
        }

        var results = await query.ToListAsync(cancellationToken);
        return results;
    }

    public async Task<Patient?> GetByNationalIdAsync(string nationalId, Guid practiceId, CancellationToken cancellationToken = default)
    {
        return await context.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(e => EF.Property<string>(e, "NationalId") == nationalId, cancellationToken);
    }

    public async Task<IEnumerable<Patient>> SearchAsync(string searchTerm, Guid practiceId, CancellationToken cancellationToken = default)
    {
        var term = searchTerm.ToLower();

        return await context.Patients
            .AsNoTracking()
            .Where(p => EF.Functions.Like(EF.Property<string>(p, "FirstName").ToLower(), $"%{term}%")
                     || EF.Functions.Like(EF.Property<string>(p, "LastName").ToLower(), $"%{term}%")
                     || EF.Functions.Like(EF.Property<string>(p, "NationalId").ToLower(), $"%{term}%"))
            .ToListAsync(cancellationToken);
    }
}