using Utano.Module.Identity.Domain.Entities;

namespace Utano.Module.Identity.Domain.Interfaces;

public interface IPracticeRepository
{
    Task AddAsync(Practice practice, CancellationToken cancellationToken = default);
    Task<Practice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
