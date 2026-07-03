using Microsoft.EntityFrameworkCore;
using Utano.Module.Core.Models;
using Utano.Module.Patients.DatabaseMappings;
using Utano.Module.Patients.Domain.Entities;
using Utano.Module.Patients.Domain.Enums;
using Utano.Module.Patients.Domain.Interfaces;
using Utano.Module.Patients.Domain.ValueObjects;

namespace Utano.Module.Patients.Infrastructure.Repositories;

public class PatientReadRepository(PatientsDbContext context) : IPatientReadRepository
{
    public async Task<Patient?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Patients
            .AsNoTracking()
            .Include(p => p.Contacts)
            .Include(p => p.Addresses)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Patient?> GetByNationalIdAsync(string nationalId,
        CancellationToken cancellationToken = default)
    {
        var nationalIdVO = NationalId.Create(nationalId);
        return await context.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.NationalId == nationalIdVO, cancellationToken);
    }

    public async Task<PagedResult<Patient>> GetPagedAsync(string? searchTerm, PatientStatus? status,
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = context.Patients.AsNoTracking();

        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(p =>
                EF.Functions.Like(p.FullName.FirstName.ToLower(), $"%{term}%") ||
                EF.Functions.Like(p.FullName.LastName.ToLower(), $"%{term}%") ||
                EF.Functions.Like(EF.Property<string>(p, "NationalId").ToLower(), $"%{term}%"));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var data = await query
            .OrderBy(p => p.FullName.LastName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Patient>
        {
            Data = data,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}
