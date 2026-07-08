using Utano.Module.Identity.Domain.Entities;

namespace Utano.Module.Identity.Domain.Interfaces;

public interface IUserWriteRepository
{
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);
    Task AddRefreshTokenAsync(Guid userId, string token, int expiryDays, CancellationToken cancellationToken = default);
}
