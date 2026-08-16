using Microsoft.EntityFrameworkCore;
using Utano.Module.Core.Services;
using Utano.Module.Identity.DatabaseMappings;

namespace Utano.Module.Identity.Infrastructure.Services;

public class UserContactLookup(IdentityDbContext context) : IUserContactLookup
{
    public async Task<string?> GetEmailAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        return user?.Email.Value;
    }
}
