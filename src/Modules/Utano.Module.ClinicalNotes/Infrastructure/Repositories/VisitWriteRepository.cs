using Utano.Module.ClinicalNotes.DatabaseMappings;
using Utano.Module.ClinicalNotes.Domain.Entities;
using Utano.Module.ClinicalNotes.Domain.Interfaces;

namespace Utano.Module.ClinicalNotes.Infrastructure.Repositories;

public class VisitWriteRepository(ClinicalNotesDbContext context) : IVisitWriteRepository
{
    public async Task AddAsync(Visit visit, CancellationToken cancellationToken = default)
    {
        await context.Visits.AddAsync(visit, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Visit visit, CancellationToken cancellationToken = default)
    {
        context.Visits.Update(visit);
        await context.SaveChangesAsync(cancellationToken);
    }
}
