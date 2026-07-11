using Utano.Module.ClinicalNotes.Domain.Entities;

namespace Utano.Module.ClinicalNotes.Domain.Interfaces;

public interface IVisitWriteRepository
{
    Task AddAsync(Visit visit, CancellationToken cancellationToken = default);
    Task UpdateAsync(Visit visit, CancellationToken cancellationToken = default);
}
