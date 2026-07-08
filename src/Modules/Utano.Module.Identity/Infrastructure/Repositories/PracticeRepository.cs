using Microsoft.EntityFrameworkCore;
using Utano.Module.Identity.DatabaseMappings;
using Utano.Module.Identity.Domain.Entities;
using Utano.Module.Identity.Domain.Interfaces;

namespace Utano.Module.Identity.Infrastructure.Repositories;

public class PracticeRepository(IdentityDbContext context) : IPracticeRepository
{
    public async Task AddAsync(Practice practice, CancellationToken cancellationToken = default)
    {
        await context.Practices.AddAsync(practice, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Practice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Practices
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }
}
