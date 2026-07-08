using Microsoft.EntityFrameworkCore;
using Utano.Module.Identity.DatabaseMappings;
using Utano.Module.Identity.Domain.Entities;
using Utano.Module.Identity.Domain.Interfaces;

namespace Utano.Module.Identity.Infrastructure.Repositories;

public class UserWriteRepository(IdentityDbContext context) : IUserWriteRepository
{
    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await context.Users.AddAsync(user, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        if (context.Entry(user).State == EntityState.Detached)
            context.Users.Update(user);

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddRefreshTokenAsync(Guid userId, string token, int expiryDays, CancellationToken cancellationToken = default)
    {
        var refreshToken = RefreshToken.Create(userId, token, expiryDays);
        await context.RefreshTokens.AddAsync(refreshToken, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }
}
