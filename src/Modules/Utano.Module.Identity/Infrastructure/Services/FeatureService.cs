using Microsoft.EntityFrameworkCore;
using Utano.Module.Core.Services;
using Utano.Module.Identity.DatabaseMappings;

namespace Utano.Module.Identity.Infrastructure.Services;

public class FeatureService(IdentityDbContext db) : IFeatureService
{
    public Task<bool> IsEnabledAsync(Guid practiceId, string featureKey, CancellationToken ct = default) =>
        db.PracticeFeatures.AnyAsync(
            pf => pf.PracticeId == practiceId && pf.FeatureKey == featureKey && pf.IsEnabled, ct);

    public async Task<IReadOnlyList<string>> GetEnabledFeaturesAsync(Guid practiceId, CancellationToken ct = default) =>
        await db.PracticeFeatures
            .Where(pf => pf.PracticeId == practiceId && pf.IsEnabled)
            .Select(pf => pf.FeatureKey)
            .ToListAsync(ct);
}
