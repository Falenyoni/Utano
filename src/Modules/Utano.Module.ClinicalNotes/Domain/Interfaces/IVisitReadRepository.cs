using Utano.Module.ClinicalNotes.Domain.Entities;
using Utano.Module.Core.Models;

namespace Utano.Module.ClinicalNotes.Domain.Interfaces;

public interface IVisitReadRepository
{
    Task<Visit?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<Visit>> GetPagedAsync(Guid? patientId, Guid? doctorId, DateOnly? date, int page, int pageSize, CancellationToken cancellationToken = default);
}
