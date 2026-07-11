using Microsoft.EntityFrameworkCore;
using Utano.Module.ClinicalNotes.DatabaseMappings;
using Utano.Module.ClinicalNotes.Domain.Entities;
using Utano.Module.ClinicalNotes.Domain.Interfaces;
using Utano.Module.Core.Models;

namespace Utano.Module.ClinicalNotes.Infrastructure.Repositories;

public class VisitReadRepository(ClinicalNotesDbContext context) : IVisitReadRepository
{
    public async Task<Visit?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Visits
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
    }

    public async Task<PagedResult<Visit>> GetPagedAsync(
        Guid? patientId, Guid? doctorId, DateOnly? date,
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = context.Visits.AsNoTracking();

        if (patientId.HasValue) query = query.Where(v => v.PatientId == patientId.Value);
        if (doctorId.HasValue) query = query.Where(v => v.DoctorId == doctorId.Value);
        if (date.HasValue) query = query.Where(v => v.VisitDate == date.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var data = await query
            .OrderByDescending(v => v.VisitDate)
            .ThenByDescending(v => v.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Visit> { Data = data, TotalCount = totalCount, Page = page, PageSize = pageSize };
    }
}
