using Microsoft.EntityFrameworkCore;
using Utano.Module.Identity.DatabaseMappings;
using Utano.Module.Identity.Domain.Entities;
using Utano.Module.Identity.Domain.Interfaces;
using Utano.Module.Identity.Domain.ValueObjects;

namespace Utano.Module.Identity.Infrastructure.Repositories;

public class UserReadRepository(IdentityDbContext context) : IUserReadRepository
{
    public async Task<User?> GetByEmailAsync(string email, Guid practiceId,
        CancellationToken cancellationToken = default)
    {
        var emailVO = Email.Create(email);
        return await context.Users
            .AsNoTracking()
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Email == emailVO && u.PracticeId == practiceId, cancellationToken);
    }

    public async Task<User?> GetByIdWithTokensAsync(Guid id,
        CancellationToken cancellationToken = default)
    {
        return await context.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<bool> EmailExistsAsync(string email, Guid practiceId,
        CancellationToken cancellationToken = default)
    {
        var emailVO = Email.Create(email);
        return await context.Users
            .AsNoTracking()
            .AnyAsync(u => u.Email == emailVO && u.PracticeId == practiceId, cancellationToken);
    }
}
