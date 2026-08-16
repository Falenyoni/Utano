using Utano.Module.Identity.Domain.Entities;

namespace Utano.Module.Identity.Domain.Interfaces;

public interface IUserWriteRepository
{
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);
    Task AddRefreshTokenAsync(Guid userId, string token, int expiryDays, CancellationToken cancellationToken = default);
    Task AddPasswordResetTokenAsync(Guid userId, string tokenHash, int expiryMinutes, CancellationToken cancellationToken = default);
    Task MarkPasswordResetTokenUsedAsync(Guid tokenId, CancellationToken cancellationToken = default);
}
