using Microsoft.EntityFrameworkCore;
using Utano.Module.Identity.DatabaseMappings;
using Utano.Module.Identity.Domain.Entities;
using Utano.Module.Identity.Domain.Enums;
using Utano.Module.Identity.Domain.Interfaces;
using Utano.Module.Identity.Domain.ValueObjects;

namespace Utano.Module.Identity.Infrastructure.Repositories;

public class UserReadRepository(IdentityDbContext context) : IUserReadRepository
{
    public async Task<User?> GetByEmailAsync(string email,
        CancellationToken cancellationToken = default)
    {
        var emailVO = Email.Create(email);
        return await context.Users
            .AsNoTracking()
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Email == emailVO, cancellationToken);
    }

    public async Task<User?> GetByIdWithTokensAsync(Guid id,
        CancellationToken cancellationToken = default)
    {
        return await context.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<bool> EmailExistsAsync(string email,
        CancellationToken cancellationToken = default)
    {
        var emailVO = Email.Create(email);
        return await context.Users
            .AsNoTracking()
            .AnyAsync(u => u.Email == emailVO, cancellationToken);
    }

    public async Task<IReadOnlyList<User>> GetByRoleAsync(Guid practiceId, UserRole role,
        CancellationToken cancellationToken = default)
    {
        return await context.Users
            .AsNoTracking()
            .Where(u => u.PracticeId == practiceId && u.Role == role)
            .OrderBy(u => u.LastName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<User>> GetAllByPracticeAsync(Guid practiceId,
        CancellationToken cancellationToken = default)
    {
        return await context.Users
            .AsNoTracking()
            .Where(u => u.PracticeId == practiceId)
            .OrderBy(u => u.Role).ThenBy(u => u.LastName)
            .ToListAsync(cancellationToken);
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Users
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }
}
