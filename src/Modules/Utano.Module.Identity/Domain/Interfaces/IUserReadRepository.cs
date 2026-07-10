using Utano.Module.Identity.Domain.Entities;
using Utano.Module.Identity.Domain.Enums;

namespace Utano.Module.Identity.Domain.Interfaces;

public interface IUserReadRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByIdWithTokensAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<User>> GetByRoleAsync(Guid practiceId, UserRole role, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<User>> GetAllByPracticeAsync(Guid practiceId, CancellationToken cancellationToken = default);
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
