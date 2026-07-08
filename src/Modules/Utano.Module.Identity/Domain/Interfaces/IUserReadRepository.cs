using Utano.Module.Identity.Domain.Entities;

namespace Utano.Module.Identity.Domain.Interfaces;

public interface IUserReadRepository
{
    Task<User?> GetByEmailAsync(string email, Guid practiceId, CancellationToken cancellationToken = default);
    Task<User?> GetByIdWithTokensAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string email, Guid practiceId, CancellationToken cancellationToken = default);
}
