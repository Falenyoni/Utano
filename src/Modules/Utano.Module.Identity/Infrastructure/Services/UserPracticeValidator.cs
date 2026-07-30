using Microsoft.EntityFrameworkCore;
using Utano.Module.Core.Services;
using Utano.Module.Identity.DatabaseMappings;

namespace Utano.Module.Identity.Infrastructure.Services;

public class UserPracticeValidator(IdentityDbContext context) : IUserPracticeValidator
{
    public async Task<bool> IsUserInPracticeAsync(
        Guid userId, Guid practiceId, CancellationToken cancellationToken = default)
    {
        return await context.Users
            .AsNoTracking()
            .AnyAsync(u => u.Id == userId && u.PracticeId == practiceId, cancellationToken);
    }
}
